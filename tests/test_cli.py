import unittest, subprocess, tempfile, json
from pathlib import Path
DLL=Path(__file__).resolve().parents[1]/'bin/Release/net10.0/LogWorkbench.dll'
class CliTests(unittest.TestCase):
 def test_filter_and_malformed(self):
  with tempfile.TemporaryDirectory() as d:
   f=Path(d)/'log';f.write_text('{"level":"ERROR","message":"Timeout"}\n{broken\nINFO ready\nWARNING retry\n',encoding='utf-8')
   p=subprocess.run(['dotnet',str(DLL),str(f),'--level','ERROR','--contains','time'],capture_output=True,text=True)
   self.assertEqual(p.returncode,0,p.stderr);r=json.loads(p.stdout);self.assertEqual((r['total'],r['matched'],r['malformed']),(4,1,1));self.assertEqual(r['counts']['WARN'],1)
 def test_bad_option(self):
  self.assertNotEqual(subprocess.run(['dotnet',str(DLL),'missing','--limit','-1'],capture_output=True).returncode,0)
