import json, os, sys, urllib.request, urllib.parse

def load_env():
    env = {}
    p = os.path.join(os.path.expanduser('~'), '.claude', 'redmine.env')
    for line in open(p, encoding='utf-8-sig'):
        line = line.strip()
        if '=' in line and not line.startswith('#'):
            k, v = line.split('=', 1)
            env[k.strip()] = v.strip().strip('"').strip("'")
    return env

ENV = load_env()
BASE = ENV['REDMINE_URL'].rstrip('/')
KEY = ENV['REDMINE_API_KEY']

def req(method, path, body=None, raw=None, ctype='application/json'):
    data = raw if raw is not None else (json.dumps(body).encode('utf-8') if body is not None else None)
    r = urllib.request.Request(BASE + path, data=data, method=method)
    r.add_header('X-Redmine-API-Key', KEY)
    if data is not None:
        r.add_header('Content-Type', ctype)
    with urllib.request.urlopen(r, timeout=30) as resp:
        txt = resp.read().decode('utf-8')
        return json.loads(txt) if txt.strip() else {}

def upload(path):
    name = os.path.basename(path)
    tok = req('POST', '/uploads.json?filename=' + urllib.parse.quote(name), raw=open(path, 'rb').read(),
              ctype='application/octet-stream')['upload']['token']
    ct = 'text/html' if name.endswith('.html') else ('text/markdown' if name.endswith('.md') else 'text/plain')
    return {'token': tok, 'filename': name, 'content_type': ct}
