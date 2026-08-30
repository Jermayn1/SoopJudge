using SoopWorkshop.Backend.Application.Common;

namespace SoopWorkshop.Tests.Unit.Application.Common
{
    public class ResultTests
    {
        [Fact]
        public void Ok_MitWert_IstErfolgreichUndOhneFehlermeldung()
        {
            var result = Result<int>.Ok(42);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(42);
            result.ErrorMessage.ShouldBeEmpty();
        }

        [Fact]
        public void Fail_MitFehlermeldung_IstNichtErfolgreichUndOhneWert()
        {
            var result = Result<int>.Fail("Aufgabe nicht gefunden");

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Aufgabe nicht gefunden");
            result.Value.ShouldBe(default);
        }

        // Ist-Verhalten: Ok() prüft den übergebenen Wert nicht auf null.
        // Bewusst nicht repariert, sondern nur festgehalten.
        [Fact]
        public void Ok_MitNull_IstAktuellErlaubt()
        {
            var result = Result<string>.Ok(null!);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBeNull();
        }
    }
}
