﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stateless.WorkflowEngine.Exceptions;
using Stateless.WorkflowEngine.Models;

namespace Stateless.WorkflowEngine.Store
{
    /// <summary>
    /// Stores workflows in memory only.  This should be used only in systems where 
    /// workflows do not need to be persisted and can be lost on system shutdown.
    /// </summary>
    public class MemoryWorkflowStore : WorkflowStore
    {
        private readonly Dictionary<Guid, Workflow> _activeWorkflows = new Dictionary<Guid, Workflow>();
        private readonly Dictionary<Guid, CompletedWorkflow> _completedWorkflows = new Dictionary<Guid, CompletedWorkflow>();

        /// <summary>
        /// Archives a workflow, moving it into the completed store.
        /// </summary>
        /// <param name="workflow">The workflow to archive.</param>
        public override void Archive(Workflow workflow)
        {
            _activeWorkflows.Remove(workflow.Id);
            _completedWorkflows.Add(workflow.Id, new CompletedWorkflow(workflow));
        }

        /// <summary>
        /// Gets all workflows of a specified type.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Workflow> GetAllByType(string workflowType)
        {
            return this._activeWorkflows.Values
                       .Where(x => x.GetType().AssemblyQualifiedName == workflowType)
                       .OrderByDescending(x => x.RetryCount)
                       .ThenBy(x => x.CreatedOn);
        }

        /// <summary>
        /// Gets a completed workflow by it's unique identifier, or null if it does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override CompletedWorkflow GetCompletedOrDefault(Guid id)
        {
            if (_completedWorkflows.ContainsKey(id))
            {
                return _completedWorkflows[id];
            }

            return null;
        }

        /// <summary>
        /// Gets an active workflow by it's unique identifier, returning null if it does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Workflow GetOrDefault(Guid id)
        {
            if (_activeWorkflows.ContainsKey(id))
            {
                return _activeWorkflows[id];
            }

            return null;
        }

        /// <summary>
        /// Gets the first <c>count</c> unsuspended active workflows, ordered by RetryCount, and then CreationDate.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override IEnumerable<Workflow> GetActive(int count)
        {
            return _activeWorkflows.Values
                .Where(x => !x.IsSuspended && x.ResumeOn <= DateTime.UtcNow)
                .OrderByDescending(x => x.RetryCount)
                .ThenBy(x => x.CreatedOn)
                .Take(count);
        }

        /// <summary>
        /// Stores a new workflow.
        /// </summary>
        /// <param name="workflow"></param>
        public override void Save(Workflow workflow)
        {
            if (_activeWorkflows.ContainsKey(workflow.Id))
            {
                _activeWorkflows[workflow.Id] = workflow;
            }
            else
            {
                _activeWorkflows.Add(workflow.Id, workflow);
            }
        }

        /// <summary>
        /// Stores a collection of new workflows.
        /// </summary>
        /// <param name="workflows">The workflows.</param>
        public override void Save(IEnumerable<Workflow> workflows)
        {
            foreach (Workflow w in workflows)
            {
                Save(w);
            }
        }



    }
}
