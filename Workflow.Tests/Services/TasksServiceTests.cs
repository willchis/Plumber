﻿using System;
using System.Collections.Generic;
using System.Linq;
using Chauffeur.TestingTools;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Workflow.Models;
using Workflow.Services;
using Workflow.Services.Interfaces;
using Xunit;

namespace Workflow.Tests.Services
{
    public class TasksServiceTests : UmbracoHostTestBase
    {
        private readonly ITasksService _service;
        private readonly IInstancesService _instancesService;

        private readonly IContentService _contentService;

        public TasksServiceTests()
        {
            Host.Run(new[] {"install y"}).Wait();

            _service = new TasksService();
            _instancesService = new InstancesService();

            _contentService = ApplicationContext.Current.Services.ContentService;

            Scaffold.Run();
            Scaffold.Config();
            Scaffold.ContentType(ApplicationContext.Current.Services.ContentTypeService);
        }

        [Fact]
        public void Can_Insert_Task_And_Raise_Event()
        {
            TasksService.Created += (sender, args) =>
            {
                Assert.NotNull(args);
                Assert.IsAssignableFrom<WorkflowTaskPoco>(args.Task);
            };

            int count = _service.CountPendingTasks();

            _service.InsertTask(Scaffold.Task());

            Assert.Equal(count + 1, _service.CountPendingTasks());
        }

        [Fact]
        public void Can_Update_Task_And_Raise_Event()
        {
            const string comment = "Comment has been updated";

            TasksService.Updated += (sender, args) =>
            {
                Assert.NotNull(args);
                Assert.IsAssignableFrom<WorkflowTaskPoco>(args.Task);
                Assert.Equal(comment, args.Task.Comment);
            };

            WorkflowTaskPoco task = Scaffold.Task();
            _service.InsertTask(task);

            task.Comment = comment;

            _service.UpdateTask(task);
        }

        [Fact]
        public void Can_Count_User_Tasks()
        {
            Guid guid = Guid.NewGuid();

            _instancesService.InsertInstance(Scaffold.Instance(guid, 1));
            _service.InsertTask(Scaffold.Task(guid));

            // status 1 is approved, there are none
            List<WorkflowTaskPoco> result = _service.GetTaskSubmissionsForUser(0, new[] { 1 });

            Assert.NotNull(result);
            Assert.Empty(result);

            // status 3 is pending approval, there should be one
            result = _service.GetTaskSubmissionsForUser(0, new[] { 3 });

            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public void Can_Get_Tasks_With_Group_By_Instance_Guid()
        {
            Guid guid = Guid.NewGuid();

            _instancesService.InsertInstance(Scaffold.Instance(guid, 1));
            _service.InsertTask(Scaffold.Task(guid));

            List<WorkflowTaskPoco> result = _service.GetTasksWithGroupByInstanceGuid(guid);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].GroupId); // same as default in scaffold.task
        }

        [Fact]
        public void Can_Get_Tasks_For_Date_Range()
        {
            _service.InsertTask(Scaffold.Task(Guid.NewGuid(), DateTime.Now.AddDays(-1)));
            List<WorkflowTaskPoco> result = _service.GetAllTasksForDateRange(DateTime.Now.AddDays(-2));

            Assert.NotNull(result);
            Assert.Single(result);

            _service.InsertTask(Scaffold.Task(Guid.NewGuid(), DateTime.Now.AddDays(-10)));
            result = _service.GetAllTasksForDateRange(DateTime.Now.AddDays(-20));

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Can_Count_Group_Tasks()
        {
            _service.InsertTask(Scaffold.Task());
            _service.InsertTask(Scaffold.Task());

            Assert.Equal(2, _service.CountGroupTasks(1));
        }

        [Fact]
        public void Can_Get_All_Pending_Tasks()
        {
            _service.InsertTask(Scaffold.Task());
            _service.InsertTask(Scaffold.Task());
            _service.InsertTask(Scaffold.Task());
            _service.InsertTask(Scaffold.Task());

            Assert.Equal(4, _service.GetAllPendingTasks(new List<int> { 1, 2, 3}).Count);
        }

        [Fact]
        public void Can_Get_Tasks_By_Node_Id()
        {
            const int nodeId = 1055;

            Guid guid = Guid.NewGuid();

            _instancesService.InsertInstance(Scaffold.Instance(guid, 1, nodeId));
            _service.InsertTask(Scaffold.Task(guid, DateTime.Now.AddDays(-1), 3, 1, 1));
            _service.InsertTask(Scaffold.Task(guid, DateTime.Now, 3, 3));

            List<WorkflowTaskPoco> result = _service.GetTasksByNodeId(nodeId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Can_Get_Tasks_By_Group_Id()
        {
            IContent node = Scaffold.Node(_contentService);

            Guid guid = Guid.NewGuid();
            _instancesService.InsertInstance(Scaffold.Instance(guid, 1, node.Id));

            _service.InsertTask(Scaffold.Task(guid, DateTime.Now.AddDays(-1), 3, 1, 1));
            _service.InsertTask(Scaffold.Task(guid, DateTime.Now, 3, 3));

            List<WorkflowTaskViewModel> result = _service.GetAllGroupTasks(3, 10, 1);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Can_Get_Pending_Workflow_Tasks()
        {
            List<WorkflowTaskViewModel> result = _service.GetPendingTasks(new List<int>
            {
                (int)TaskStatus.PendingApproval
            }, 10, 1);

            Assert.NotNull(result);
            Assert.Empty(result);

            var i = 0;
            while (i < 5)
            {
                IContent node = Scaffold.Node(_contentService);

                Guid guid = Guid.NewGuid();
                _instancesService.InsertInstance(Scaffold.Instance(guid, 1, node.Id));

                _service.InsertTask(Scaffold.Task(guid));
                i += 1;
            }

            List<WorkflowTaskViewModel> result2 = _service.GetPendingTasks(new List<int>
            {
                (int)TaskStatus.PendingApproval
            }, 10, 1);

            Assert.NotNull(result2);
            Assert.Equal(i, result2.Count);
        }

        [Fact]
        public void Can_Get_Task_By_id()
        {
            IContent root = Scaffold.Node(_contentService);

            Guid guid = Guid.NewGuid();

            _instancesService.InsertInstance(Scaffold.Instance(guid, 1, root.Id));
            _service.InsertTask(Scaffold.Task(guid));

            List<WorkflowTaskViewModel> tasks = _service.GetPendingTasks(new List<int>
            {
                (int)TaskStatus.PendingApproval
            }, 10, 1);
            int id = tasks.First().TaskId;

            WorkflowTaskViewModel taskViewModel = _service.GetTask(id);

            Assert.NotNull(taskViewModel);
            Assert.Equal(id, taskViewModel.TaskId);
        }

        /// <summary>
        /// Filter is a taskstatus int, as a string. Not sure why...
        /// </summary>
        [Fact]
        public void Can_Get_Paged_Filtered_Tasks()
        {
            IContent node = Scaffold.Node(_contentService);

            Guid guid = Guid.NewGuid();
            _instancesService.InsertInstance(Scaffold.Instance(guid, 1, node.Id));

            _service.InsertTask(Scaffold.Task(guid));
            _service.InsertTask(Scaffold.Task(guid));
            _service.InsertTask(Scaffold.Task(guid));
            _service.InsertTask(Scaffold.Task(guid));
            _service.InsertTask(Scaffold.Task(guid, DateTime.Now, 2, 1, 1));

            List<WorkflowTaskViewModel> tasks = _service.GetFilteredPagedTasksForDateRange(DateTime.Now.AddDays(-2), 2, 1);

            Assert.NotEmpty(tasks);
            Assert.Equal(2, tasks.Count);

            tasks = _service.GetFilteredPagedTasksForDateRange(DateTime.Now.AddDays(-2), 1, 1, "1");

            Assert.NotEmpty(tasks);
            Assert.Single(tasks);
        }
    }
}
