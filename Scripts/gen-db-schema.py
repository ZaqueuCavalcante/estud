"""Gera Web/app/utils/db-schema.ts a partir dos IEntityTypeConfiguration do backend.

Uso: python3 scripts/gen-db-schema.py [saida.ts]
"""

import re, os, json, sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.join(REPO, "Back")
TS_PATH = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, "Web/app/utils/db-schema.ts")

SCALARS = {
    "int","long","short","byte","bool","string","decimal","double","float",
    "DateTime","DateOnly","TimeOnly","TimeSpan","Guid","char","JsonDocument",
    "byte[]","int[]","string[]","object",
}

# enums found in the codebase
ENUMS = set("""BackoffStrategy BrazilState CalendarDaySource ClassActivityStatus ClassActivityType
ClassActivityWorkStatus ClassLessonStatus ClassNoteType ClassStatus CommandBatchStatus CommandBatchType
CommandStatus CourseSession CourseType Day DayType DomainEventStatus FeatureGroup Hour NotificationType
ParentRelationship ParentStudentStatus PermissionGroup ReceivedWebhookEventSource ReceivedWebhookEventStatus
Shift SocialLoginProvider SsoProviderType StorageContainer StudentClassStatus StudentDayAttendanceStatus
StudentDisciplineStatus StudentStatus UserActivitySeverity UserActivityType UserType UsersGroup
WebhookCallAttemptStatus WebhookCallStatus WebhookEventType""".split())

def snake(name):
    s = re.sub(r'(?<=[a-z0-9])(?=[A-Z])', '_', name)
    s = re.sub(r'(?<=[A-Z])(?=[A-Z][a-z])', '_', s)
    return s.lower()

def read(p):
    with open(p, encoding='utf-8-sig') as f:
        return f.read()

cs_files = []
for dirpath, _, names in os.walk(ROOT):
    if '/obj' in dirpath or '/bin' in dirpath:
        continue
    for n in names:
        if n.endswith('.cs'):
            cs_files.append(os.path.join(dirpath, n))

# ---------- entity class properties ----------

PROP_RE = re.compile(
    r'public\s+(?:virtual\s+|required\s+)?'
    r'(?P<type>[A-Za-z_][\w\.]*(?:<[^>{}()=;]*>)?(?:\[\])?\??)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*'
    r'(?=\{\s*(?:get|init))'
)

class_props = {}   # class name -> [(propName, type)]
class_base = {}    # class name -> base type string

for path in cs_files:
    src = read(path)
    for m in re.finditer(r'public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)(?:\s*:\s*([^\n{]+))?', src):
        cname = m.group(1)
        base = (m.group(2) or '').strip()
        rest = src[m.end():]
        bodyless = base.endswith(';') or re.match(r'\s*;', rest) is not None
        base = base.rstrip(';').strip()
        if bodyless:
            if cname not in class_props:
                class_props[cname] = []
                class_base[cname] = base
            continue
        start = src.find('{', m.end())
        if start == -1:
            continue
        depth = 0
        i = start
        while i < len(src):
            if src[i] == '{': depth += 1
            elif src[i] == '}':
                depth -= 1
                if depth == 0: break
            i += 1
        body = src[start:i]
        props = [(pm.group('name'), pm.group('type')) for pm in PROP_RE.finditer(body)]
        if cname not in class_props or len(props) > len(class_props[cname]):
            class_props[cname] = props
            class_base[cname] = base

# ASP.NET Identity base-class columns (not present in this repo's source)
IDENTITY_BASE = {
    'IdentityUser': [
        ("Id","int"),("UserName","string?"),("NormalizedUserName","string?"),("Email","string?"),
        ("NormalizedEmail","string?"),("EmailConfirmed","bool"),("PasswordHash","string?"),
        ("SecurityStamp","string?"),("ConcurrencyStamp","string?"),("PhoneNumber","string?"),
        ("PhoneNumberConfirmed","bool"),("TwoFactorEnabled","bool"),("LockoutEnd","DateTimeOffset?"),
        ("LockoutEnabled","bool"),("AccessFailedCount","int"),
    ],
    'IdentityRole': [
        ("Id","int"),("Name","string?"),("NormalizedName","string?"),("ConcurrencyStamp","string?"),
    ],
    'IdentityUserClaim': [("Id","int"),("UserId","int"),("ClaimType","string?"),("ClaimValue","string?")],
    'IdentityRoleClaim': [("Id","int"),("RoleId","int"),("ClaimType","string?"),("ClaimValue","string?")],
    'IdentityUserLogin': [("LoginProvider","string"),("ProviderKey","string"),("ProviderDisplayName","string?"),("UserId","int")],
    'IdentityUserToken': [("LoginProvider","string"),("Name","string"),("Value","string?"),("UserId","int")],
    'IdentityUserRole': [("UserId","int"),("RoleId","int")],
    'DataProtectionKey': [("Id","int"),("FriendlyName","string?"),("Xml","string?")],
}

def all_props(cname, seen=None):
    seen = seen or set()
    if cname in IDENTITY_BASE and not class_props.get(cname) and not class_base.get(cname):
        return IDENTITY_BASE[cname]
    if cname in seen: return []
    seen.add(cname)
    out = []
    base = class_base.get(cname, '')
    if base:
        first = base.split(',')[0].strip()
        gen = re.match(r'(\w+)<', first)
        raw = gen.group(1) if gen else first
        if cname in IDENTITY_BASE and not class_props.get(cname):
            return IDENTITY_BASE[cname]
        if raw in IDENTITY_BASE:
            out += IDENTITY_BASE[raw]
        elif raw in class_props:
            out += all_props(raw, seen)
    out += class_props.get(cname, [])
    # dedupe keeping last
    dd = {}
    for n, t in out: dd[n] = t
    return list(dd.items())

def is_scalar(t):
    base = t.rstrip('?')
    if base.startswith('List<') or base.startswith('ICollection<') or base.startswith('IEnumerable<'):
        return False
    if base in SCALARS or base in ENUMS: return True
    if base == 'DateTimeOffset': return True
    if base.startswith('Dictionary<'): return False
    return False

PG = {
    'int':'integer','long':'bigint','short':'smallint','byte':'smallint','bool':'boolean',
    'string':'text','decimal':'numeric','double':'double precision','float':'real',
    'DateTime':'timestamp with time zone','DateTimeOffset':'timestamp with time zone',
    'DateOnly':'date','TimeOnly':'time without time zone','TimeSpan':'interval',
    'Guid':'uuid','char':'character(1)','byte[]':'bytea','JsonDocument':'jsonb',
    'string[]':'text[]','int[]':'integer[]',
}

def pg_type(t, enum_hint=False):
    base = t.rstrip('?')
    if base in ENUMS: return 'integer'
    return PG.get(base, base)

# ---------- db configs ----------

def brace_body(src, idx):
    start = src.find('{', idx)
    depth, i = 0, start
    while i < len(src):
        if src[i] == '{': depth += 1
        elif src[i] == '}':
            depth -= 1
            if depth == 0: return src[start:i]
        i += 1
    return src[start:]

def lambda_members(expr):
    expr = expr.strip()
    m = re.match(r'^\s*\w+\s*=>\s*(.*)$', expr, re.S)
    if not m: return []
    body = m.group(1).strip()
    if body.startswith('new'):
        inner = body[body.find('{')+1:body.rfind('}')]
        return [p.strip().split('.')[-1] for p in inner.split(',') if p.strip()]
    return [body.split('.')[-1].strip()]

tables = {}

for path in cs_files:
    src = read(path)
    m = re.search(r'IEntityTypeConfiguration<(\w+)>', src)
    if not m: continue
    entity = m.group(1)
    tm = re.search(r'ToTable\(\s*"([a-z0-9_]+)"', src)
    if not tm: continue
    table = tm.group(1)

    cfg = brace_body(src, src.find('Configure(EntityTypeBuilder'))

    pk = []
    km = re.search(r'\.HasKey\(([^;]*?)\)\s*;', cfg, re.S)
    if km: pk = lambda_members(km.group(1))
    if not pk and any(n == 'Id' for n, _ in all_props(entity)):
        pk = ['Id']

    indexes = []
    for im in re.finditer(r'\.HasIndex\((.*?)\)\s*((?:\.\w+\([^)]*\)\s*)*);', cfg, re.S):
        cols = lambda_members(im.group(1))
        indexes.append({'columns': cols, 'unique': 'IsUnique' in (im.group(2) or '')})

    rels = []   # (owner_entity_or_nav, target, columns, principal, nav)

    def statements(body):
        out, depth, cur = [], 0, []
        for ch in body:
            if ch in '([{': depth += 1
            elif ch in ')]}': depth -= 1
            if ch == ';' and depth == 0:
                out.append(''.join(cur)); cur = []
            else:
                cur.append(ch)
        if ''.join(cur).strip(): out.append(''.join(cur))
        return out

    def nav_target(owner, nav):
        for n, t in all_props(owner):
            if n == nav:
                base = t.rstrip('?')
                gm = re.match(r'(?:List|ICollection|IEnumerable)<(\w+)>', base)
                return gm.group(1) if gm else base
        return None

    for st in statements(cfg.strip().lstrip('{').rstrip('}')):
        if '.HasForeignKey' not in st:
            continue

        um = re.search(r'\.UsingEntity<(\w+)>', st)
        if um:
            join_entity = um.group(1)
            for hm in re.finditer(
                r'\.HasOne(?:<(\w+)>)?\(\s*(?:\w+\s*=>\s*\w+\.(\w+))?\s*\)[^;]*?\.HasForeignKey\(([^)]*)\)',
                st, re.S):
                tgt = hm.group(1) or (nav_target(join_entity, hm.group(2)) if hm.group(2) else None)
                rels.append({
                    'owner': join_entity, 'target': tgt,
                    'columns': lambda_members(hm.group(3)), 'principal': [], 'nav': hm.group(2),
                })
            continue

        pkm = re.search(r'\.HasPrincipalKey(?:<\w+>)?\((.*?)\)\s*\.', st, re.S)
        principal = lambda_members(pkm.group(1)) if pkm else []

        fkm = re.search(r'\.HasForeignKey(?:<(\w+)>)?\((.*?)\)\s*(?:\.\w+\([^)]*\)\s*)*$', st.strip(), re.S)
        if not fkm:
            fkm = re.search(r'\.HasForeignKey(?:<(\w+)>)?\((.*?)\)', st, re.S)
        if not fkm:
            continue
        explicit_owner = fkm.group(1)
        cols = lambda_members(fkm.group(2))
        if not cols:
            continue

        many = re.search(r'\.HasMany\(\s*\w+\s*=>\s*\w+\.(\w+)\s*\)', st)
        one = re.search(r'\.HasOne(?:<(\w+)>)?\(\s*(?:\w+\s*=>\s*\w+\.(\w+))?\s*\)', st)

        if explicit_owner:
            nav_name = one.group(2) if (one and one.group(2)) else None
            other = (one.group(1) if one else None) or (nav_target(entity, nav_name) if nav_name else None)
            target = other if explicit_owner == entity else entity
            rels.append({'owner': explicit_owner, 'target': target, 'columns': cols,
                         'principal': principal, 'nav': nav_name if explicit_owner == entity else None})
        elif many:
            owner = nav_target(entity, many.group(1))
            if owner:
                rels.append({'owner': owner, 'target': entity, 'columns': cols, 'principal': principal, 'nav': None})
        elif one:
            target = one.group(1) or (nav_target(entity, one.group(2)) if one.group(2) else None)
            rels.append({'owner': entity, 'target': target, 'columns': cols, 'principal': principal, 'nav': one.group(2)})

    props = {}
    for pm in re.finditer(r'\.Property\(\s*\w+\s*=>\s*\w+\.(\w+)\s*\)((?:\s*\.\w+\([^;]*?\))*)\s*;', cfg, re.S):
        props[pm.group(1)] = pm.group(2)

    tables[table] = {
        'table': table, 'entity': entity, 'pk': pk, 'indexes': indexes,
        'rels': rels, 'props': props, 'file': os.path.relpath(path, os.path.dirname(ROOT)),
    }

entity_to_table = {v['entity']: k for k, v in tables.items()}

for t in tables.values():
    t['fks'] = []

for t in list(tables.values()):
    for r in t['rels']:
        owner_table = entity_to_table.get(r['owner'])
        if not owner_table:
            continue
        tables[owner_table]['fks'].append({
            'columns': r['columns'], 'target': r['target'],
            'principal': r['principal'], 'nav': r['nav'],
        })

for t in tables.values():
    declared = {tuple(fk['columns']) for fk in t['fks']}
    declared_cols = {c for fk in t['fks'] for c in fk['columns']}
    props = dict(all_props(t['entity']))
    for name, ctype in props.items():
        if is_scalar(ctype): continue
        base = ctype.rstrip('?')
        if re.match(r'(?:List|ICollection|IEnumerable)<', base): continue
        if base not in entity_to_table: continue
        fk_prop = f'{name}Id'
        if fk_prop not in props or not is_scalar(props[fk_prop]): continue
        if (fk_prop,) in declared or fk_prop in declared_cols: continue
        t['fks'].append({
            'columns': [fk_prop], 'target': base, 'principal': [], 'nav': name, 'convention': True,
        })
        declared.add((fk_prop,))

for t in tables.values():
    seen, dedup = set(), []
    for fk in t['fks']:
        k = (tuple(fk['columns']), fk['target'])
        if k in seen: continue
        seen.add(k); dedup.append(fk)
    t['fks'] = dedup

# ---------- build column lists ----------

out = []
for table, t in sorted(tables.items()):
    entity = t['entity']
    raw = all_props(entity)
    cols = []
    for name, ctype in raw:
        cfgs = t['props'].get(name, '')
        converted = 'HasColumnType' in cfgs or 'HasConversion' in cfgs
        if not is_scalar(ctype) and not converted:
            continue
        col = {
            'name': snake(name),
            'prop': name,
            'type': pg_type(ctype),
            'nullable': ctype.endswith('?'),
            'clr': ctype,
        }
        if ctype.rstrip('?') in ENUMS:
            col['enum'] = ctype.rstrip('?')
        cm = re.search(r'HasColumnType\(\s*"([^"]+)"', cfgs)
        if cm: col['type'] = cm.group(1)
        pm = re.search(r'HasPrecision\(\s*(\d+)\s*,\s*(\d+)\s*\)', cfgs)
        if pm: col['type'] = f'numeric({pm.group(1)},{pm.group(2)})'
        mm = re.search(r'HasMaxLength\(\s*(\d+)\s*\)', cfgs)
        if mm: col['type'] = f'varchar({mm.group(1)})'
        if 'IsRequired()' in cfgs: col['nullable'] = False
        if 'ValueGeneratedNever' in cfgs: col['generated'] = False
        dm = re.search(r'HasDefaultValue\(\s*([^)]*?)\s*\)', cfgs)
        if dm: col['default'] = dm.group(1)
        if 'HasConversion' in cfgs: col['converted'] = True
        cols.append(col)

    pk_cols = [snake(p) for p in t['pk']]
    fk_list = []
    for fk in t['fks']:
        fk_list.append({
            'convention': bool(fk.get('convention')),
            'columns': [snake(c) for c in fk['columns']],
            'target': entity_to_table.get(fk['target'] or '', None),
            'targetEntity': fk['target'],
            'nav': fk['nav'],
            'principal': [snake(p) for p in fk['principal']],
        })
    idx_list = [{'columns': [snake(c) for c in i['columns']], 'unique': i['unique']} for i in t['indexes']]

    out.append({
        'table': table, 'entity': entity, 'pk': pk_cols,
        'columns': cols, 'fks': fk_list, 'indexes': idx_list, 'file': t['file'],
    })

print(f"{len(out)} tabelas, {sum(len(t['columns']) for t in out)} colunas, {sum(len(t['fks']) for t in out)} fks")
for t in out:
    if not t['columns']:
        print("  SEM COLUNAS:", t['table'], t['entity'])
    if not t['pk']:
        print("  SEM PK:", t['table'])

# ---------- TS emit ----------

if TS_PATH:
    def ts(v):
        return json.dumps(v, ensure_ascii=False)

    lines = [
        '// Gerado por scripts/gen-db-schema.py a partir dos IEntityTypeConfiguration do backend.',
        '// Não editar à mão: rode `python3 scripts/gen-db-schema.py <json> <ts>` para atualizar.',
        '',
        'export interface DbColumn {',
        '  name: string',
        '  prop: string',
        '  type: string',
        '  clr: string',
        '  nullable: boolean',
        '  enum?: string',
        '  default?: string',
        '  converted?: boolean',
        '}',
        '',
        'export interface DbForeignKey {',
        '  columns: string[]',
        '  target: string | null',
        '  targetEntity: string | null',
        '  principal: string[]',
        '  nav: string | null',
        '  convention: boolean',
        '}',
        '',
        'export interface DbIndex {',
        '  columns: string[]',
        '  unique: boolean',
        '}',
        '',
        'export interface DbTable {',
        '  table: string',
        '  entity: string',
        '  file: string',
        '  pk: string[]',
        '  columns: DbColumn[]',
        '  fks: DbForeignKey[]',
        '  indexes: DbIndex[]',
        '}',
        '',
        'export const dbSchema: DbTable[] = [',
    ]

    for t in out:
        lines.append('  {')
        lines.append(f'    table: {ts(t["table"])},')
        lines.append(f'    entity: {ts(t["entity"])},')
        lines.append(f'    file: {ts(t["file"].replace(os.sep, "/"))},')
        lines.append(f'    pk: {ts(t["pk"])},')
        lines.append('    columns: [')
        for c in t['columns']:
            parts = [f'name: {ts(c["name"])}', f'prop: {ts(c["prop"])}', f'type: {ts(c["type"])}',
                     f'clr: {ts(c["clr"])}', f'nullable: {ts(c["nullable"])}']
            if c.get('enum'): parts.append(f'enum: {ts(c["enum"])}')
            if 'default' in c: parts.append(f'default: {ts(c["default"])}')
            if c.get('converted'): parts.append('converted: true')
            lines.append('      { ' + ', '.join(parts) + ' },')
        lines.append('    ],')
        lines.append('    fks: [')
        for f in t['fks']:
            parts = [f'columns: {ts(f["columns"])}', f'target: {ts(f["target"])}',
                     f'targetEntity: {ts(f["targetEntity"])}', f'principal: {ts(f["principal"])}',
                     f'nav: {ts(f["nav"])}', f'convention: {ts(f["convention"])}']
            lines.append('      { ' + ', '.join(parts) + ' },')
        lines.append('    ],')
        lines.append('    indexes: [')
        for i in t['indexes']:
            lines.append(f'      {{ columns: {ts(i["columns"])}, unique: {ts(i["unique"])} }},')
        lines.append('    ],')
        lines.append('  },')

    lines.append(']')
    lines.append('')
    with open(TS_PATH, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    print(f'TS: {TS_PATH}')
