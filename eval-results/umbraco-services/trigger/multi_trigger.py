"""Register EVERY published skill's description side by side (as in a real install), run each query
with claude -p in its own temp project, and record which skill(s) Claude invokes in the first turns.
Usage: multi_trigger.py REPO_ROOT QUERIES_JSON OUT_JSON   (queries: [{"query", "expect"}], expect = skill name or "none")"""
import json, os, re, subprocess, sys, uuid, shutil, tempfile, glob
from concurrent.futures import ThreadPoolExecutor
repo, qfile, out = sys.argv[1:4]
RUNS, MAX_TURNS, TIMEOUT = 3, 4, 240

def desc_of(path):
    s = open(path).read().split("---")[1]
    m = re.search(r"description:\s*>\s*\n((?:\s+.*\n?)+)", s)
    return " ".join(l.strip() for l in m.group(1).splitlines() if l.strip())

skills = {os.path.basename(os.path.dirname(p)): desc_of(p)
          for p in glob.glob(f"{repo}/plugins/*/skills/*/SKILL.md")}

def one(query):
    root = tempfile.mkdtemp(prefix="multi-")
    os.makedirs(f"{root}/.claude/commands")
    tag = uuid.uuid4().hex[:6]
    names = {}
    for skill, desc in skills.items():
        cmd = f"{skill}-{tag}"
        names[cmd] = skill
        open(f"{root}/.claude/commands/{cmd}.md", "w").write(
            f"---\ndescription: |\n  {desc}\n---\n\n# {skill}\n\nThis skill handles: {desc}\n")
    env = {k: v for k, v in os.environ.items() if k != "CLAUDECODE"}
    try:
        p = subprocess.run(["claude", "-p", query, "--output-format", "stream-json", "--verbose",
                            "--max-turns", str(MAX_TURNS)], cwd=root, env=env,
                           capture_output=True, text=True, timeout=TIMEOUT)
        lines = p.stdout.splitlines()
    except subprocess.TimeoutExpired as e:
        lines = (e.stdout.decode(errors="replace") if isinstance(e.stdout, bytes) else (e.stdout or "")).splitlines()
    invoked, tools = [], 0
    for l in lines:
        try: ev = json.loads(l)
        except Exception: continue
        if ev.get("type") != "assistant": continue
        for c in ev["message"]["content"]:
            if c.get("type") != "tool_use": continue
            tools += 1
            inp = json.dumps(c.get("input", {}))
            for cmd, skill in names.items():
                if cmd in inp and skill not in invoked: invoked.append(skill)
    shutil.rmtree(root, ignore_errors=True)
    return invoked, tools

queries = json.load(open(qfile))
jobs = [(i, q["query"]) for i, q in enumerate(queries) for _ in range(RUNS)]
with ThreadPoolExecutor(6) as ex:
    res = list(ex.map(lambda j: (j[0], one(j[1])), jobs))
results = []
for i, q in enumerate(queries):
    runs = [r for k, r in res if k == i]
    results.append({**q, "runs": [{"invoked": inv, "tool_calls": t} for inv, t in runs]})
json.dump({"skills": sorted(skills), "results": results}, open(out, "w"), indent=1)
for r in results:
    firsts = [x["invoked"][0] if x["invoked"] else "none" for x in r["runs"]]
    hit = sum(f == r["expect"] for f in firsts)
    print(f"{hit}/{len(firsts)} expect={r['expect']:<28} got={firsts}  {r['query'][:60]}")
