﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// This enum controls the actions requested for an entity.
    /// </summary>
    /// <remarks>
    /// This is required because during the post-CUD events, the EntityState has been lost.
    /// This enum allows the API to remember which pre-CUD event was raised for the Entity.
    /// </remarks>
    public enum DataModificationItemAction
    {
        /// <summary>
        /// Specifies an undefined action.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Specifies the entity is being updated.
        /// </summary>
        Update,

        /// <summary>
        /// Specifies the entity is being inserted.
        /// </summary>
        Insert,

        /// <summary>
        /// Specifies the entity is being removed.
        /// </summary>
        Remove
    }

    /// <summary>
    /// Specifies the type of a change set item.
    /// </summary>
    internal enum ChangeSetItemType
    {
        /// <summary>
        /// Specifies a data modification item.
        /// </summary>
        DataModification
    }

    /// <summary>
    /// Possible states of an entity during a ChangeSet life cycle
    /// </summary>
    internal enum ChangeSetItemProcessingStage
    {
        /// <summary>
        /// If an new change set item is created
        /// </summary>
        Initialized,

        /// <summary>
        /// The entity has been validated.
        /// </summary>
        Validated,

        /// <summary>
        /// The entity set deleting, inserting or updating events are raised
        /// </summary>
        PreEventing,

        /// <summary>
        /// The entity was modified within its own pre eventing interception method. This indicates that the entity
        /// should be revalidated but its pre eventing interception point should not be invoked again.
        /// </summary>
        ChangedWithinOwnPreEventing,

        /// <summary>
        /// The entity's pre events have been raised
        /// </summary>
        PreEvented
    }

    /// <summary>
    /// Represents an item in a change set.
    /// </summary>
    public abstract class ChangeSetItem
    {
        internal ChangeSetItem(ChangeSetItemType type)
        {
            this.Type = type;
            this.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.Initialized;
        }

        /// <summary>
        /// Gets the type of this change set item.
        /// </summary>
        internal ChangeSetItemType Type { get; private set; }

        /// <summary>
        /// Gets or sets the dynamic state of this change set item.
        /// </summary>
        internal ChangeSetItemProcessingStage ChangeSetItemProcessingStage { get; set; }

        /// <summary>
        /// Indicates whether this change set item is in a changed state.
        /// </summary>
        /// <returns>
        /// Whether this change set item is in a changed state.
        /// </returns>
        public bool HasChanged()
        {
            return this.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Initialized ||
                this.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing;
        }
    }

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    public class DataModificationItem : ChangeSetItem
    {
        private const string IfNoneMatchKey = "@IfNoneMatchKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem" /> class.
        /// </summary>
        /// <param name="entitySetName">
        /// The name of the entity set in question.
        /// </param>
        /// <param name="expectedEntityType">
        /// The type of the expected entity type in question.
        /// </param>
        /// <param name="actualEntityType">
        /// The type of the actual entity type in question.
        /// </param>
        /// <param name="action">
        /// The DataModificationItemAction for the request.
        /// </param>
        /// <param name="entityKey">
        /// The key of the entity being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the entity that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationItem(
            string entitySetName,
            Type expectedEntityType,
            Type actualEntityType,
            DataModificationItemAction action,
            IReadOnlyDictionary<string, object> entityKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(ChangeSetItemType.DataModification)
        {
            Ensure.NotNull(entitySetName, "entitySetName");
            Ensure.NotNull(expectedEntityType, "expectedEntityType");
            this.EntitySetName = entitySetName;
            this.ExpectedEntityType = expectedEntityType;
            this.ActualEntityType = actualEntityType;
            this.EntityKey = entityKey;
            this.OriginalValues = originalValues;
            this.LocalValues = localValues;
            this.DataModificationItemAction = action;
        }

        /// <summary>
        /// Gets the name of the entity set in question.
        /// </summary>
        public string EntitySetName { get; private set; }

        /// <summary>
        /// Gets the name of the expected entity type in question.
        /// </summary>
        public Type ExpectedEntityType { get; private set; }

        /// <summary>
        /// Gets the name of the actual entity type in question.
        /// In type inheritance case, this is different from expectedEntityType
        /// </summary>
        public Type ActualEntityType { get; private set; }

        /// <summary>
        /// Gets the key of the entity being modified.
        /// </summary>
        public IReadOnlyDictionary<string, object> EntityKey { get; private set; }

        /// <summary>
        /// Gets or sets the action to be taken.
        /// </summary>
        public DataModificationItemAction DataModificationItemAction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity should be fully replaced by the modification.
        /// </summary>
        /// <remarks>
        /// If true, all properties will be updated, even if the property isn't in LocalValues.
        /// If false, only properties identified in LocalValues will be updated on the entity.
        /// </remarks>
        public bool IsFullReplaceUpdateRequest { get; set; }

        /// <summary>
        /// Gets or sets the entity object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending entity.
        /// </remarks>
        public object Entity { get; set; }

        /// <summary>
        /// Gets the original values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> OriginalValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current server values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>. For updated
        /// entities, it is <c>null</c> until the change set is prepared.
        /// </remarks>
        public IReadOnlyDictionary<string, object> ServerValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the local values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For entities pending deletion, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> LocalValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Applies the current DataModificationItem's KeyValues and OriginalValues to the
        /// specified query and returns the new query.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The new IQueryable with the property values applied to it in a Where condition.
        /// </returns>
        public IQueryable ApplyTo(IQueryable query)
        {
            Ensure.NotNull(query, "query");
            if (this.DataModificationItemAction == DataModificationItemAction.Insert)
            {
                throw new InvalidOperationException(Resources.DataModificationNotSupportCreateEntity);
            }

            Type type = query.ElementType;
            ParameterExpression param = Expression.Parameter(type);
            Expression where = null;

            if (this.EntityKey != null)
            {
                foreach (KeyValuePair<string, object> item in this.EntityKey)
                {
                    where = ApplyPredicate(param, where, item);
                }
            }

            if (where == null)
            {
                throw new InvalidOperationException(Resources.DataModificationRequiresEntityKey);
            }

            LambdaExpression whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, type);
        }

        /// <summary>
        /// Applies the current DataModificationItem's OriginalValues to the
        /// specified query and returns the new query.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The new IQueryable with the property values applied to it in a Where condition.
        /// </returns>
        public IQueryable ApplyEtag(IQueryable query)
        {
            Ensure.NotNull(query, "query");
            Type type = query.ElementType;
            ParameterExpression param = Expression.Parameter(type);
            Expression where = null;

            if (this.OriginalValues != null)
            {
                foreach (KeyValuePair<string, object> item in this.OriginalValues)
                {
                    if (!item.Key.StartsWith("@", StringComparison.Ordinal))
                    {
                        where = ApplyPredicate(param, where, item);
                    }
                }

                if (this.OriginalValues.ContainsKey(IfNoneMatchKey))
                {
                    where = Expression.Not(where);
                }
            }

            LambdaExpression whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, type);
        }

        private static Expression ApplyPredicate(
            ParameterExpression param,
            Expression where,
            KeyValuePair<string, object> item)
        {
            MemberExpression property = Expression.Property(param, item.Key);
            object itemValue = item.Value;

            if (itemValue.GetType() != property.Type)
            {
                itemValue = Convert.ChangeType(itemValue, property.Type, CultureInfo.InvariantCulture);
            }

            // TODO GitHubIssue#31 : Check if LinqParameterContainer is necessary
            // Expression value = itemValue != null
            //     ? LinqParameterContainer.Parameterize(itemValue.GetType(), itemValue)
            //     : Expression.Constant(value: null);
            var constant = Expression.Constant(itemValue, property.Type);
            BinaryExpression equal = Expression.Equal(property, constant);
            return where == null ? equal : Expression.AndAlso(where, equal);
        }
    }

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class DataModificationItem<T> : DataModificationItem
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem{T}" /> class.
        /// </summary>
        /// <param name="entitySetName">
        /// The name of the entity set in question.
        /// </param>
        /// <param name="expectedEntityType">
        /// The type of the expected entity type in question.
        /// </param>
        /// <param name="actualEntityType">
        /// The type of the actual entity type in question.
        /// </param>
        /// <param name="action">
        /// The DataModificationItemAction for the request.
        /// </param>
        /// <param name="entityKey">
        /// The key of the entity being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the entity that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationItem(
            string entitySetName,
            Type expectedEntityType,
            Type actualEntityType,
            DataModificationItemAction action,
            IReadOnlyDictionary<string, object> entityKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(entitySetName, expectedEntityType, actualEntityType, action, entityKey, originalValues, localValues)
        {
        }

        /// <summary>
        /// Gets or sets the entity object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending entity.
        /// </remarks>
        public new T Entity
        {
            get
            {
                return base.Entity as T;
            }

            set
            {
                base.Entity = value;
            }
        }
    }
}
