"""PSE full 50-slot verification over VRChat OSC (9000 send / 9001 listen).

Phases:
  A) Save pattern A into all 50 slots   (PE/Set=i,     PE/Float=i/100)
  B) Load-verify all 50 slots against pattern A
  C) Overwrite all 50 slots with pattern B (PE/Set=100+i, PE/Float=(51-i)/100)
  D) Load-verify all 50 slots against pattern B (proves overwrite, old values gone)
  E) Jump persistence: load a slot, jump via /input/Jump, verify PE values survive
"""
import socket, struct, time

TX = ("127.0.0.1", 9000)
SAVE = lambda i: i
LOAD = lambda i: 100 + i
TOL = 0.02

def pad(b): return b + b"\x00" * (4 - len(b) % 4)
def msg_i(a, v): return pad(a.encode()) + pad(b",i") + struct.pack(">i", v)
def msg_f(a, v): return pad(a.encode()) + pad(b",f") + struct.pack(">f", v)

def parse(data):
    try:
        z = data.index(b"\x00")
        addr = data[:z].decode(errors="replace")
        rest = data[(z // 4 + 1) * 4:]
        if rest.startswith(b",i"):
            return addr, struct.unpack(">i", rest[4:8])[0]
        if rest.startswith(b",f"):
            return addr, struct.unpack(">f", rest[4:8])[0]
        return addr, None
    except Exception:
        return "<?>", None

rx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
rx.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
rx.bind(("127.0.0.1", 9001))
rx.settimeout(0.05)
tx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

latest = {}

def pump(seconds):
    end = time.time() + seconds
    while time.time() < end:
        try:
            data, _ = rx.recvfrom(65535)
        except socket.timeout:
            continue
        a, v = parse(data)
        latest[a] = v

def send(m, settle=0.15):
    tx.sendto(m, TX)
    pump(settle)

def wait_for(addr, want, timeout=2.5):
    """Pump until latest[addr] equals want (with tolerance for floats)."""
    end = time.time() + timeout
    while time.time() < end:
        pump(0.1)
        v = latest.get(addr)
        if v is None:
            continue
        if isinstance(want, float):
            if abs(v - want) <= TOL:
                return True
        elif v == want:
            return True
    return False

def set_pose(s, f):
    send(msg_i("/avatar/parameters/PE/Set", s))
    send(msg_f("/avatar/parameters/PE/Float", f))
    ok = wait_for("/avatar/parameters/PE/Set", s) and \
         wait_for("/avatar/parameters/PE/Float", float(f))
    return ok

def command(c, settle=0.35):
    send(msg_i("/avatar/parameters/PSE/Command", c), settle)
    send(msg_i("/avatar/parameters/PSE/Command", 0), 0.15)

def save_pass(tag, set_of, float_of):
    fails = []
    for i in range(1, 51):
        if not set_pose(set_of(i), float_of(i)):
            fails.append((i, "set-pose"))
            continue
        command(SAVE(i))
        slot = f"/avatar/parameters/PSE/{i:02d}/"
        ok = wait_for(slot + "Set", set_of(i)) and \
             wait_for(slot + "Pose", float(float_of(i)))
        if not ok:
            fails.append((i, f"slot-echo got Set={latest.get(slot+'Set')} Pose={latest.get(slot+'Pose')}"))
    print(f"[{tag}] save fails:", fails if fails else "none (50/50 ok)")
    return fails

def load_pass(tag, set_of, float_of):
    fails = []
    for i in range(1, 51):
        # scramble current pose so a passing check can't be a leftover
        send(msg_i("/avatar/parameters/PE/Set", 255))
        send(msg_f("/avatar/parameters/PE/Float", 0.999))
        wait_for("/avatar/parameters/PE/Set", 255)
        command(LOAD(i), settle=0.5)
        ok = wait_for("/avatar/parameters/PE/Set", set_of(i)) and \
             wait_for("/avatar/parameters/PE/Float", float(float_of(i)))
        if not ok:
            fails.append((i, f"got Set={latest.get('/avatar/parameters/PE/Set')} "
                             f"Float={latest.get('/avatar/parameters/PE/Float')}"))
    print(f"[{tag}] load fails:", fails if fails else "none (50/50 ok)")
    return fails

def jump_test():
    # real BuddyWorks pose so persistence is meaningful in-game
    set_pose(7, 0.25)
    command(SAVE(1))
    set_pose(3, 0.5)
    command(LOAD(1), settle=0.6)
    ok_load = wait_for("/avatar/parameters/PE/Set", 7) and \
              wait_for("/avatar/parameters/PE/Float", 0.25)
    # jump: press and release
    send(msg_i("/input/Jump", 1), 0.3)
    send(msg_i("/input/Jump", 0), 0.2)
    time.sleep(2.5)
    pump(1.5)
    s, f = latest.get("/avatar/parameters/PE/Set"), latest.get("/avatar/parameters/PE/Float")
    survived = s == 7 and f is not None and abs(f - 0.25) <= TOL
    print(f"[E] jump: load_ok={ok_load} after_jump PE/Set={s} PE/Float={None if f is None else round(f,4)}"
          f" -> {'POSE_SURVIVED_JUMP' if survived else 'POSE_LOST_AFTER_JUMP'}")
    return survived

t0 = time.time()
pump(1.0)
a_set, a_float = (lambda i: i), (lambda i: round(i / 100, 2))
b_set, b_float = (lambda i: 100 + i), (lambda i: round((51 - i) / 100, 2))

fa = save_pass("A:save", a_set, a_float)
fb = load_pass("B:loadA", a_set, a_float)
fc = save_pass("C:overwrite", b_set, b_float)
fd = load_pass("D:loadB", b_set, b_float)
je = jump_test()

total_fail = len(fa) + len(fb) + len(fc) + len(fd) + (0 if je else 1)
print(f"ELAPSED {round(time.time()-t0)}s")
print("FINAL:", "ALL_PASS" if total_fail == 0 else f"FAILURES={total_fail}")
