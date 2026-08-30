using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Services;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Backend.Application.Submissions.Services;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Application.Tasks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SoopWorkshop.Backend.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITaskCategoryService, TaskCategoryService>();
            services.AddScoped<ITaskItemService, TaskItemService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IEvaluationService, EvaluationService>();
            services.AddScoped<ITaskTestService, TaskTestService>();
            services.AddScoped<ITaskUnitTestFileService, TaskUnitTestFileService>();
            services.AddScoped<ITaskCategoryWeightService, TaskCategoryWeightService>();

            return services;
        }
    }
}