using ServerInfo;
using Xunit;

// Deliberately no namespace: GameReflection looks types up by simple name,
// which is how it finds Valheim's own (also namespace-less) game types
public class ProbeType
{
    private static string StaticSecret = "static-value";
    private string instanceSecret = "instance-value";
    private string InstanceProp { get; set; } = "prop-value";
    private string Reveal() => "revealed";
}

namespace ServerInfo.Tests
{
    public class GameReflectionTests
    {
        [Fact]
        public void FindType_FindsTypeInCurrentAppDomain()
        {
            Assert.NotNull(GameReflection.FindType("ProbeType"));
        }

        [Fact]
        public void FindType_ReturnsNullForUnknownType()
        {
            Assert.Null(GameReflection.FindType("Definitely_Not_A_Real_Type_12345"));
        }

        [Fact]
        public void GetStaticMember_ReadsPrivateStaticField()
        {
            var type = GameReflection.FindType("ProbeType");
            Assert.Equal("static-value", GameReflection.GetStaticMember(type, "StaticSecret"));
        }

        [Fact]
        public void GetMember_ReadsPrivateInstanceFieldAndProperty()
        {
            var instance = new ProbeType();
            Assert.Equal("instance-value", GameReflection.GetMember(instance, "instanceSecret"));
            Assert.Equal("prop-value", GameReflection.GetMember(instance, "InstanceProp"));
        }

        [Fact]
        public void GetMember_ReturnsNullForNullTarget()
        {
            Assert.Null(GameReflection.GetMember(null, "anything"));
        }

        [Fact]
        public void Invoke_CallsPrivateInstanceMethod()
        {
            var instance = new ProbeType();
            Assert.Equal("revealed", GameReflection.Invoke(instance, "Reveal"));
        }

        [Fact]
        public void Invoke_ReturnsNullForNullTarget()
        {
            Assert.Null(GameReflection.Invoke(null, "anything"));
        }
    }
}
