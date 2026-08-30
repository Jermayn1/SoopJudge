using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class TaskCategoryServiceTests
    {
        private readonly ITaskCategoryRepository _repository = Substitute.For<ITaskCategoryRepository>();

        private TaskCategoryService CreateService() =>
            new(_repository, NullLogger<TaskCategoryService>.Instance);

        private static TaskItem Task(string title, int order) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Order = order,
            Difficulty = Difficulty.Easy,
            EvaluationMode = EvaluationMode.ConsoleOnly
        };

        private static TaskCategory Category(string name, params TaskItem[] tasks) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Order = 1,
            IsVisible = true,
            IconName = "Layers",
            Tasks = tasks
        };

        // Die Aufgaben einer Kategorie bauen aufeinander auf. Kommen sie in der
        // Reihenfolge der Datenbank heraus, ist das keine Reihenfolge, sondern
        // Zufall: ohne ORDER BY entscheidet PostgreSQL je Abfrage neu.
        [Fact]
        public async Task GetAllAsync_AufgabenUnsortiert_LiefertSieNachOrder()
        {
            _repository.GetAllAsync().Returns([
                Category("Schleifen", Task("Dritte", 3), Task("Erste", 1), Task("Zweite", 2))
            ]);

            var result = await CreateService().GetAllAsync();

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Single().Tasks
                .Select(t => t.Title)
                .ShouldBe(["Erste", "Zweite", "Dritte"]);
        }

        // Die öffentliche Seite fragt über diesen Weg. Griffe er auf GetAllAsync
        // zurück, sähen Teilnehmer jede noch unfertige Kategorie.
        [Fact]
        public async Task GetAllVisibleAsync_FragtDasRepositoryNachDenSichtbaren()
        {
            _repository.GetAllVisibleAsync().Returns([Category("Grundlagen")]);

            var result = await CreateService().GetAllVisibleAsync();

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Single().Name.ShouldBe("Grundlagen");
            await _repository.DidNotReceive().GetAllAsync();
        }

        [Fact]
        public async Task GetByIdAsync_KategorieVorhanden_LiefertAlleFelder()
        {
            var category = Category("OOP", Task("Bankkonto", 1));
            _repository.GetByIdAsync(category.Id).Returns(category);

            var result = await CreateService().GetByIdAsync(category.Id);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Id.ShouldBe(category.Id);
            result.Value.Name.ShouldBe("OOP");
            result.Value.Order.ShouldBe(1);
            result.Value.IsVisible.ShouldBeTrue();
            result.Value.IconName.ShouldBe("Layers");
            result.Value.Tasks.Single().Title.ShouldBe("Bankkonto");
        }

        [Fact]
        public async Task GetByIdAsync_KategorieFehlt_LiefertFehler()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskCategory?)null);

            var result = await CreateService().GetByIdAsync(Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Kategorie");
        }

        // Neu angelegt wird immer verborgen. Erst füllen, dann sichtbar
        // schalten - sonst steht eine leere Kategorie in der Teilnehmersicht.
        [Fact]
        public async Task CreateAsync_NeueKategorie_IstZunaechstVerborgen()
        {
            var result = await CreateService().CreateAsync(new CreateTaskCategoryDto
            {
                Name = "Schleifen",
                Order = 2,
                IconName = "Repeat"
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.IsVisible.ShouldBeFalse();
            result.Value.Name.ShouldBe("Schleifen");
            result.Value.Order.ShouldBe(2);
            result.Value.IconName.ShouldBe("Repeat");

            await _repository.Received(1).AddAsync(Arg.Is<TaskCategory>(c =>
                c.Name == "Schleifen" && c.IconName == "Repeat" && !c.IsVisible));
        }

        [Fact]
        public async Task UpdateAsync_KategorieVorhanden_UebertraegtAlleFelder()
        {
            var category = Category("Alt");
            category.IsVisible = false;
            _repository.GetByIdAsync(category.Id).Returns(category);

            var result = await CreateService().UpdateAsync(new UpdateTaskCategoryDto
            {
                Id = category.Id,
                Name = "Neu",
                Order = 7,
                IconName = "Braces",
                IsVisible = true
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Name.ShouldBe("Neu");
            result.Value.Order.ShouldBe(7);
            result.Value.IconName.ShouldBe("Braces");
            result.Value.IsVisible.ShouldBeTrue();

            await _repository.Received(1).UpdateAsync(category);
        }

        [Fact]
        public async Task UpdateAsync_KategorieFehlt_SpeichertNichts()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskCategory?)null);

            var result = await CreateService().UpdateAsync(new UpdateTaskCategoryDto
            {
                Id = Guid.NewGuid(),
                Name = "Neu"
            });

            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<TaskCategory>());
        }

        [Fact]
        public async Task DeleteAsync_KategorieVorhanden_LoeschtSie()
        {
            var category = Category("Weg damit");
            _repository.GetByIdAsync(category.Id).Returns(category);

            var result = await CreateService().DeleteAsync(category.Id);

            result.IsSuccess.ShouldBeTrue();
            await _repository.Received(1).DeleteAsync(category.Id);
        }

        [Fact]
        public async Task DeleteAsync_KategorieFehlt_LoeschtNichts()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskCategory?)null);

            var result = await CreateService().DeleteAsync(Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task ToggleVisibilityAsync_DrehtDenStandUmUndLiefertIhnZurueck(
            bool vorher, bool nachher)
        {
            var category = Category("Grundlagen");
            category.IsVisible = vorher;
            _repository.GetByIdAsync(category.Id).Returns(category);

            var result = await CreateService().ToggleVisibilityAsync(category.Id);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(nachher);
            category.IsVisible.ShouldBe(nachher);
            await _repository.Received(1).UpdateAsync(category);
        }

        [Fact]
        public async Task ToggleVisibilityAsync_KategorieFehlt_SpeichertNichts()
        {
            _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((TaskCategory?)null);

            var result = await CreateService().ToggleVisibilityAsync(Guid.NewGuid());

            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<TaskCategory>());
        }
    }
}
