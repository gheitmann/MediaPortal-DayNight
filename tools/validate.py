from pathlib import Path
import xml.etree.ElementTree as ET
r=Path(__file__).resolve().parents[1]
root=ET.parse(r/'src/Languages.xml').getroot()
keys={t.attrib['key'] for t in root.find("language[@code='en']")}
assert len(keys)==18
langs=root.findall('language'); assert len(langs)==37
assert len({x.attrib['code'].lower() for x in langs})==37
for lang in langs:
    entries=lang.findall('text'); assert len(entries)==18
    assert {t.attrib['key'] for t in entries}==keys
    assert all(t.text and t.text.strip() for t in entries)
marker=ET.parse(r/'examples/skin-support.xml').getroot()
assert marker.tag=='dayNightSkin' and marker.attrib['contract']=='1'
assert marker.attrib['periods']=='morning,noon,day,night'
assert len(marker.findall('asset'))==4
assert all(a.get('path','').startswith('Backgrounds/') for a in marker)
assert ET.parse(r/'examples/DayNight.xml').findtext('id')=='87950'
assert len(list((r/'src').glob('*.cs')))==6
for name in ('README.md','LICENSE','CHANGELOG.md','docs/SKIN-INTEGRATION.md','docs/VALIDATION.md'):
    assert (r/name).read_text(encoding='utf-8').strip()
print('PASS: 37 languages x 18 strings, six source files, contract example and documentation.')
