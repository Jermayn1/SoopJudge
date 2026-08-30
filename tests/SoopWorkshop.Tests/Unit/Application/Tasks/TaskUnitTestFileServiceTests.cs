using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class TaskUnitTestFileServiceTests
    {
        private readonly ITaskUnitTestFileRepository _repository =
            Substitute.For<ITaskUnitTestFileRepository>();

        private readonly ITaskItemRepository _taskItemRepository =
            Substitute.For<ITaskItemRepository>();

        private TaskUnitTestFileService CreateService() =>
            new(_repository, _taskItemRepository, NullLogger<TaskUnitTestFileService>.Instance);

        private Guid GivenExistingTask()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(true);
            return taskItemId;
        }

        private static SaveTaskUnitTestFileEntryDto Entry(string fileName, int order) => new()
        {
            FileName = fileName,
            Content = "class Test {}",
            Order = order,
            IsVisibleToParticipant = false
        };

        // --- Dateiname ---------------------------------------------------------

        // Der Name landet als echte Datei im Arbeitsverzeichnis und muss in Java
        // zum Klassennamen passen. Fällt das erst bei javac auf, sieht es für
        // den Teilnehmer nach einem Fehler in seiner Abgabe aus.
        [Fact]
        public async Task CreateAsync_KeineJavaDatei_LehntAbUndNenntDenNamen()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(new CreateTaskUnitTestFileDto
            {
                TaskItemId = taskItemId,
                FileName = "MainTest.txt",
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("MainTest.txt");
            result.ErrorMessage.ShouldContain(".java");
            await _repository.DidNotReceive().AddAsync(Arg.Any<TaskUnitTestFile>());
        }

        [Theory]
        [InlineData("unterordner/MainTest.java")]
        [InlineData("unterordner\\MainTest.java")]
        [InlineData("../MainTest.java")]
        public async Task CreateAsync_DateinameMitPfadanteil_LehntAb(string fileName)
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(new CreateTaskUnitTestFileDto
            {
                TaskItemId = taskItemId,
                FileName = fileName,
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Pfadangaben");
            await _repository.DidNotReceive().AddAsync(Arg.Any<TaskUnitTestFile>());
        }

        [Fact]
        public async Task CreateAsync_EndungInGrossbuchstaben_WirdAkzeptiert()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(new CreateTaskUnitTestFileDto
            {
                TaskItemId = taskItemId,
                FileName = "MainTest.JAVA",
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeTrue();
        }

        // Der Name wird vor dem Laden geprüft. Ein ungültiger Name darf die
        // Datenbank gar nicht erst beschäftigen.
        [Fact]
        public async Task UpdateAsync_KeineJavaDatei_LaedtNichtsUndSpeichertNichts()
        {
            var result = await CreateService().UpdateAsync(new UpdateTaskUnitTestFileDto
            {
                Id = Guid.NewGuid(),
                FileName = "MainTest.txt",
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<TaskUnitTestFile>());
        }

        // --- Fremdschlüssel ---------------------------------------------------

        [Fact]
        public async Task CreateAsync_AufgabeExistiertNicht_LiefertFehlerStattFremdschluesselverletzung()
        {
            _taskItemRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().CreateAsync(new CreateTaskUnitTestFileDto
            {
                TaskItemId = Guid.NewGuid(),
                FileName = "MainTest.java",
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");
            await _repository.DidNotReceive().AddAsync(Arg.Any<TaskUnitTestFile>());
        }

        [Fact]
        public async Task SaveAllAsync_AufgabeExistiertNicht_ErsetztNichts()
        {
            _taskItemRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = Guid.NewGuid(),
                Files = [Entry("MainTest.java", 1)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");
            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskUnitTestFile>>());
        }

        // --- Anlegen und Ändern -----------------------------------------------

        [Fact]
        public async Task CreateAsync_GueltigeDatei_LegtSieAnUndGibtSieZurueck()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(new CreateTaskUnitTestFileDto
            {
                TaskItemId = taskItemId,
                FileName = "MainTest.java",
                Content = "class MainTest {}",
                Order = 3,
                IsVisibleToParticipant = true
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.FileName.ShouldBe("MainTest.java");
            result.Value.TaskItemId.ShouldBe(taskItemId);
            result.Value.Order.ShouldBe(3);
            result.Value.IsVisibleToParticipant.ShouldBeTrue();

            await _repository.Received(1).AddAsync(Arg.Is<TaskUnitTestFile>(f =>
                f.FileName == "MainTest.java" && f.IsVisibleToParticipant));
        }

        [Fact]
        public async Task UpdateAsync_DateiVorhanden_UebertraegtAlleFelder()
        {
            var file = new TaskUnitTestFile
            {
                Id = Guid.NewGuid(),
                TaskItemId = Guid.NewGuid(),
                FileName = "AltTest.java",
                Content = "alt",
                Order = 1,
                IsVisibleToParticipant = false
            };
            _repository.GetByIdAsync(file.Id).Returns(file);

            var result = await CreateService().UpdateAsync(new UpdateTaskUnitTestFileDto
            {
                Id = file.Id,
                FileName = "NeuTest.java",
                Content = "neu",
                Order = 5,
                IsVisibleToParticipant = true
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.FileName.ShouldBe("NeuTest.java");
            result.Value.Content.ShouldBe("neu");
            result.Value.Order.ShouldBe(5);
            result.Value.IsVisibleToParticipant.ShouldBeTrue();

            await _repository.Received(1).UpdateAsync(file);
        }

        [Fact]
        public async Task UpdateAsync_DateiFehlt_SpeichertNichts()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskUnitTestFile?)null);

            var result = await CreateService().UpdateAsync(new UpdateTaskUnitTestFileDto
            {
                Id = Guid.NewGuid(),
                FileName = "MainTest.java",
                Content = "class MainTest {}"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("JUnit-Datei");
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<TaskUnitTestFile>());
        }

        [Fact]
        public async Task DeleteAsync_DateiFehlt_LoeschtNichts()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskUnitTestFile?)null);

            var result = await CreateService().DeleteAsync(Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
        }

        // --- Blockspeicherung --------------------------------------------------

        [Fact]
        public async Task SaveAllAsync_MehrereDateien_ErsetztDenBestandInEinemZug()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = taskItemId,
                Files = [Entry("KontoTest.java", 1), Entry("KundeTest.java", 2)]
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Count.ShouldBe(2);

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskUnitTestFile>>(files => files.Count == 2));
        }

        // Eine leere Liste ist eine gültige Angabe: sie löscht alle Dateien.
        // Das braucht der Editor, wenn der Modus auf ConsoleOnly wechselt.
        [Fact]
        public async Task SaveAllAsync_LeereListe_LoeschtAlleDateien()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = taskItemId,
                Files = []
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.ShouldBeEmpty();

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskUnitTestFile>>(files => files.Count == 0));
        }

        // Beide Dateien landen im selben Arbeitsverzeichnis - die eine würde die
        // andere überschreiben. Groß- und Kleinschreibung zählt dabei nicht,
        // weil das Dateisystem unter Windows sie auch nicht unterscheidet.
        [Fact]
        public async Task SaveAllAsync_DoppelterDateiname_LehntAbUnabhaengigVonDerSchreibweise()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = taskItemId,
                Files = [Entry("MainTest.java", 1), Entry("maintest.java", 2)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("mehrfach");
            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskUnitTestFile>>());
        }

        [Fact]
        public async Task SaveAllAsync_EinEintragOhneJavaEndung_ErsetztNichts()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = taskItemId,
                Files = [Entry("KontoTest.java", 1), Entry("notiz.txt", 2)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("notiz.txt");
            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskUnitTestFile>>());
        }

        // Festgehaltenes Ist-Verhalten: anders als TaskTestService lehnt
        // SaveAllAsync doppelte Order-Werte nicht ab. Die Anzeigereihenfolge hängt
        // dann von der Datenbank ab. Die beiden Schwesterservices weichen hier
        // also voneinander ab - wer das angleicht, muss diesen Test umdrehen.
        [Fact]
        public async Task SaveAllAsync_DoppelteReihenfolge_WirdBislangNichtBeanstandet()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskUnitTestFilesDto
            {
                TaskItemId = taskItemId,
                Files = [Entry("KontoTest.java", 1), Entry("KundeTest.java", 1)]
            });

            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task GetByTaskItemIdAsync_LiefertDieDateienDerAufgabe()
        {
            var taskItemId = Guid.NewGuid();
            _repository.GetByTaskItemIdAsync(taskItemId).Returns([
                new TaskUnitTestFile
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = taskItemId,
                    FileName = "MainTest.java",
                    Content = "class MainTest {}",
                    Order = 1,
                    IsVisibleToParticipant = true
                }
            ]);

            var result = await CreateService().GetByTaskItemIdAsync(taskItemId);

            result.IsSuccess.ShouldBeTrue();

            var file = result.Value!.ShouldHaveSingleItem();
            file.FileName.ShouldBe("MainTest.java");
            file.IsVisibleToParticipant.ShouldBeTrue();
        }
    }
}
