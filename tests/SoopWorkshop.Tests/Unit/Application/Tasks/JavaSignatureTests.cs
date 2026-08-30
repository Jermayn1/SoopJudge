using SoopWorkshop.Backend.Application.Tasks;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class JavaSignatureTests
    {
        [Theory]
        [InlineData("public static int addiere(int a, int b)", "addiere")]
        [InlineData("public static void main(String[] args)", "main")]
        [InlineData("int addiere(int, int)", "addiere")]
        [InlineData("addiere(int a, int b)", "addiere")]
        [InlineData("addiere()", "addiere")]
        [InlineData("addiere", "addiere")]
        [InlineData("  public   static   int   addiere  (int a)  ", "addiere")]
        public void ExtractMethodName_LiefertDenReinenNamen(string signature, string expected)
        {
            JavaSignature.ExtractMethodName(signature).ShouldBe(expected);
        }

        // Generische Rückgabetypen dürfen den Namen nicht verschlucken.
        [Fact]
        public void ExtractMethodName_MitGenerischemRueckgabetyp_LiefertDenNamen()
        {
            JavaSignature.ExtractMethodName("public List<String> lies(int anzahl)").ShouldBe("lies");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ExtractMethodName_LeereSignatur_LiefertLeerenNamen(string signature)
        {
            JavaSignature.ExtractMethodName(signature).ShouldBeEmpty();
        }
    }
}
