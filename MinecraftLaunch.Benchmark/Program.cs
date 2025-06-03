using BenchmarkDotNet.Running;
using MinecraftLaunch.Benchmark;
using MinecraftLaunch.Components.Authenticator;
using System.Security.Cryptography;
using System.Text;

byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + "Yang114"));

hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

Console.WriteLine("Uuid:" + Guid.Parse(new Uuid(hash).ToString()));
Console.WriteLine("Uuid1:" + Guid.Parse(new Uuid1(hash).ToString()));

return;
var summary = BenchmarkRunner.Run<UuidBenchmarkTest>();
Console.WriteLine(summary);