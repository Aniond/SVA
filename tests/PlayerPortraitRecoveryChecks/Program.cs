using SolaceWeather.PlayerPortraits;
var recovery = new PortraitCaptureRecovery(); int calls = 0;
bool Broken() { calls++; throw new InvalidOperationException("temporary graphics failure"); }
if (recovery.TryCapture(Broken, false)) throw new Exception("Failed capture accepted.");
for (int i = 0; i < 30; i++) recovery.TryCapture(Broken, false);
if (calls != 1) throw new Exception("Automatic capture retry loop.");
if (!recovery.TryCapture(() => { calls++; return true; }, true) || calls != 2) throw new Exception("Explicit retry cannot recover.");
Console.WriteLine("PASS: transient capture failure, no automatic loop, explicit recovery.");
