using Microsoft.AspNetCore.Http;
using SoopWorkshop.Backend.API.Validation;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.API.Validation
{
    public class SubmissionUploadValidatorTests
    {
        [Fact]
        public void Validate_GueltigeJavaDatei_LiefertKeineFehler()
        {
            List<IFormFile> files = [FormFileFactory.Create("Main.java")];

            SubmissionUploadValidator.Validate(files).ShouldBeEmpty();
        }

        [Fact]
        public void Validate_KeineDatei_MeldetFehlendenUpload()
        {
            SubmissionUploadValidator.Validate([]).ShouldHaveSingleItem().ShouldContain("keine Datei");
        }

        [Theory]
        [InlineData("Bild.png")]
        [InlineData("Main.class")]
        [InlineData("Main")]
        [InlineData("Main.java.txt")]
        public void Validate_FalscheEndung_WirdAbgelehnt(string fileName)
        {
            List<IFormFile> files = [FormFileFactory.Create(fileName)];

            SubmissionUploadValidator.Validate(files).ShouldNotBeEmpty();
        }

        // Groß-/Kleinschreibung der Endung soll niemanden aufhalten.
        [Fact]
        public void Validate_EndungInGrossbuchstaben_WirdAkzeptiert()
        {
            List<IFormFile> files = [FormFileFactory.Create("Main.JAVA")];

            SubmissionUploadValidator.Validate(files).ShouldBeEmpty();
        }

        // Der Dateiname landet als Pfadbestandteil im Arbeitsverzeichnis.
        [Theory]
        [InlineData("../Main.java")]
        [InlineData("..\\..\\Main.java")]
        [InlineData("/etc/passwd.java")]
        [InlineData("unterordner/Main.java")]
        [InlineData("   ")]
        public void Validate_DateinameMitPfadanteil_WirdAbgelehnt(string fileName)
        {
            List<IFormFile> files = [FormFileFactory.Create(fileName)];

            SubmissionUploadValidator.Validate(files).ShouldNotBeEmpty();
        }

        [Fact]
        public void Validate_ZuGrosseDatei_WirdAbgelehnt()
        {
            List<IFormFile> files =
                [FormFileFactory.CreateWithSize("Main.java", SubmissionUploadLimits.MaxFileSizeBytes + 1)];

            SubmissionUploadValidator.Validate(files).ShouldHaveSingleItem().ShouldContain("größer");
        }

        [Fact]
        public void Validate_LeereDatei_WirdAbgelehnt()
        {
            List<IFormFile> files = [FormFileFactory.Create("Main.java", content: string.Empty)];

            SubmissionUploadValidator.Validate(files).ShouldHaveSingleItem().ShouldContain("leer");
        }

        [Fact]
        public void Validate_ZuVieleDateien_WirdAbgelehnt()
        {
            var files = Enumerable
                .Range(1, SubmissionUploadLimits.MaxFileCount + 1)
                .Select(index => FormFileFactory.Create($"Datei{index}.java"))
                .ToList();

            SubmissionUploadValidator.Validate(files).ShouldHaveSingleItem().ShouldContain("höchstens");
        }

        // Gleichnamige Dateien würden sich im Arbeitsverzeichnis still überschreiben.
        [Fact]
        public void Validate_ZweiDateienMitGleichemNamen_WirdAbgelehnt()
        {
            List<IFormFile> files = [FormFileFactory.Create("Main.java"), FormFileFactory.Create("Main.java")];

            SubmissionUploadValidator.Validate(files).ShouldHaveSingleItem().ShouldContain("mehrfach");
        }

        // Der Vergleich läuft über OrdinalIgnoreCase, weil das Dateisystem unter
        // Windows es genauso hält und javac bei gleichem Namen die eine Klasse
        // mit der anderen überschreibt.
        //
        // checkFiles in uploadLimits.ts vergleicht genauso - beide Seiten müssen
        // hier übereinstimmen, sonst lehnt der Server etwas ab, das die
        // Oberfläche gerade noch durchgelassen hat.
        [Fact]
        public void Validate_GleicherNameInAndererSchreibweise_WirdEbenfallsAbgelehnt()
        {
            List<IFormFile> files = [FormFileFactory.Create("Main.java"), FormFileFactory.Create("main.java")];

            SubmissionUploadValidator.Validate(files).ShouldHaveSingleItem().ShouldContain("mehrfach");
        }

        [Fact]
        public void Validate_MehrereVerstoesse_MeldetAlleAufEinmal()
        {
            List<IFormFile> files =
            [
                FormFileFactory.Create("Bild.png"),
                FormFileFactory.Create("Main.java", content: string.Empty)
            ];

            SubmissionUploadValidator.Validate(files).Count.ShouldBe(2);
        }

        [Fact]
        public void Validate_MehrereGueltigeDateien_LiefertKeineFehler()
        {
            List<IFormFile> files =
                [FormFileFactory.Create("Main.java"), FormFileFactory.Create("Helfer.java")];

            SubmissionUploadValidator.Validate(files).ShouldBeEmpty();
        }
    }
}
