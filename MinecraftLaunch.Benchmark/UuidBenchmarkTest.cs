using BenchmarkDotNet.Attributes;
using MinecraftLaunch.Components.Authenticator;
using System.Security.Cryptography;
using System.Text;

namespace MinecraftLaunch.Benchmark;

[MemoryDiagnoser]
public class UuidBenchmarkTest {
    [Benchmark]
    public Guid Test1() {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + "Yang114"));

        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

        return Guid.Parse(new Uuid(hash).ToString());
    }

    [Benchmark]
    public Guid Test2() {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + "Yang114"));

        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

        return Guid.Parse(new Uuid1(hash).ToString());
    }
}