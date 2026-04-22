from pathlib import Path
from pypdf import PdfReader

source_pdf = Path('Docs/User Story Tower Defense.pdf')
target_md = Path('Docs/User Story Tower Defense.md')

if not source_pdf.exists():
    raise FileNotFoundError(f"{source_pdf} not found")

reader = PdfReader(str(source_pdf))
text = "\n\n".join(page.extract_text() or '' for page in reader.pages)
lines = [line.strip() for line in text.splitlines() if line.strip()]

split_index = 0
for i, line in enumerate(lines):
    if line.startswith('Afin de'):
        split_index = i
        break

us_rows = lines[1:split_index]
req_rows = lines[split_index+1:]

us_table = []
for row in us_rows:
    parts = row.split()
    if len(parts) >= 4 and parts[0].startswith('US'):
        id_ = parts[0]
        tenant = parts[1]
        want = ' '.join(parts[2:])
        us_table.append((id_, tenant, want))

req_table = []
for row in req_rows:
    parts = row.split()
    if len(parts) >= 2:
        status = parts[-1]
        goal = ' '.join(parts[:-1])
        req_table.append((goal, status))

lines_out = []
lines_out.append('# User Story Tower Defense')
lines_out.append('')
lines_out.append('## User Stories')
lines_out.append('')
lines_out.append('| ID | En tant que | Je veux |')
lines_out.append('| --- | --- | --- |')
for id_, tenant, want in us_table:
    lines_out.append(f'| {id_} | {tenant} | {want} |')
lines_out.append('')
lines_out.append('## Critères / exigences')
lines_out.append('')
lines_out.append('| Afin de | Priorité |')
lines_out.append('| --- | --- |')
for goal, status in req_table:
    lines_out.append(f'| {goal} | {status} |')
lines_out.append('')

target_md.write_text('\n'.join(lines_out), encoding='utf-8')
print(f'Saved {target_md}')
