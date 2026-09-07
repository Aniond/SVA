using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley.Locations;
var method=typeof(MineShaft).GetMethod(nameof(MineShaft.UpdateWhenCurrentLocation),new[]{typeof(Microsoft.Xna.Framework.GameTime)})!;
var opcodes=typeof(OpCodes).GetFields(BindingFlags.Static|BindingFlags.Public).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>(ushort)o.Value);
var bytes=method.GetMethodBody()!.GetILAsByteArray()!;var instructions=new List<CodeInstruction>();
for(int i=0;i<bytes.Length;)
{
    ushort code=bytes[i++];if(code==0xfe)code=(ushort)(0xfe00|bytes[i++]);var op=opcodes[code];object? operand=null;
    int size=op.OperandType switch {OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+BitConverter.ToInt32(bytes,i)*4,_=>4};
    if(op.OperandType==OperandType.InlineString)operand=method.Module.ResolveString(BitConverter.ToInt32(bytes,i));
    if(op.OperandType==OperandType.InlineMethod)operand=method.Module.ResolveMethod(BitConverter.ToInt32(bytes,i));
    instructions.Add(new CodeInstruction(op,operand));i+=size;
}
var type=typeof(AbigailModern.Visuals.CaveAtmosphereController);
var transformed=((IEnumerable<CodeInstruction>)type.GetMethod("RouteNativeDripCall",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{instructions})!).ToArray();
int routed=transformed.Count(i=>i.operand is MethodInfo m && m.DeclaringType==type && m.Name=="PlayNativeDrip");
if(routed!=1)throw new Exception("Expected one exact native drip route.");
Console.WriteLine("PASS: actual installed MineShaft IL routes exactly one cavedrip call; no runtime patch or audio played.");
