// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public struct LobbyFilter
    {
        internal string LogicOperator => logicOperator;
        internal string Key => key;
        internal List<object> Values => values;
        internal string FilterOperatorToString(FilterOperator filterOperator) => filterToStringMapping[filterOperator];
        internal string FilterGroupOperatorToString(FilterGroupOperator filterGroupOperator) =>
            filterGroupOperator.ToString().ToLowerInvariant();

        [JsonProperty("op")]
        private string logicOperator;

        [JsonProperty("key")]
        private string key;

        [JsonProperty("value")]
        private List<object> values;

        private static Dictionary<FilterOperator, string> filterToStringMapping = new Dictionary<FilterOperator, string>()
        {
            { FilterOperator.Equals ,"=" },
            { FilterOperator.NotEquals ,"!=" },
            { FilterOperator.LessThan ,"<" },
            { FilterOperator.LessOrEqualThan ,"<=" },
            { FilterOperator.GreaterThan ,">" },
            { FilterOperator.GreaterOrEqualThan ,">=" },
            { FilterOperator.Any ,"any" },
            { FilterOperator.Between ,"between" }
        };

        private Stack<LobbyFilter> filterBuilderStack;

        public override string ToString()
        {
            var strBuilder = new StringBuilder();

            return GetExpressionString(this, strBuilder, false);
        }

        private static string GetExpressionString(LobbyFilter filter, StringBuilder stringBuilder, bool useParenthesis)
        {
            if (filter.IsFilterGroup())
            {
                if (useParenthesis)
                {
                    stringBuilder.Append("(");
                }

                for (int i = 0; i < filter.values.Count; i++)
                {
                    LobbyFilter nestedFilter = (LobbyFilter)filter.values[i];

                    GetExpressionString(nestedFilter, stringBuilder, true);

                    if (i != filter.values.Count - 1)
                    {
                        stringBuilder.Append($" {filter.logicOperator} ");
                    }
                }

                if (useParenthesis)
                {
                    stringBuilder.Append(")");
                }
            }

            if (filter.IsNonFilterGroup())
            {
                stringBuilder.AppendJoin(" ",
                    new List<string>() { filter.key, filter.logicOperator, " " });

                if (filter.values.Count > 1)
                {
                    stringBuilder.Append("[");
                }

                for (int i = 0; i < filter.values.Count; i++)
                {
                    var value = filter.values[i];
                    stringBuilder.Append(value.ToString());

                    if (i != filter.values.Count - 1)
                    {
                        stringBuilder.Append(", ");
                    }
                }

                if (filter.values.Count > 1)
                {
                    stringBuilder.Append("]");
                }
            }

            return stringBuilder.ToString();
        }

        public LobbyFilter WithAnd()
        {
            return WithFilterGroup(FilterGroupOperator.And);
        }

        public LobbyFilter WithOr()
        {
            return WithFilterGroup(FilterGroupOperator.Or);
        }

        public LobbyFilter End()
        {
            if (filterBuilderStack == null)
            {
                throw new InvalidOperationException("End must be used within a nested filter group.");
            }

            filterBuilderStack.Pop();

            if (!filterBuilderStack.TryPeek(out var parent))
            {
                throw new InvalidOperationException("End must be used within a nested filter group.");
            }

            return parent;
        }

        public LobbyFilter WithRegion(FilterOperator op, IEnumerable<string> regions)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.region.ToString();
                values = new List<object>(regions);
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = op.ToString().ToLowerInvariant(),
                    key = FilterKey.region.ToString(),
                    values = new List<object>(regions)
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithTag(FilterOperator op, List<string> tags)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.tag.ToString();
                values = new List<object>(tags);
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.tag.ToString(),
                    values = new List<object>(tags)
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithMaxPlayers(FilterOperator op, int maxPlayers)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.maxPlayers.ToString();
                values = new List<object> { maxPlayers };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.maxPlayers.ToString(),
                    values = new List<object> { maxPlayers }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithNumPlayers(FilterOperator op, int numPlayers)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.numPlayers.ToString();
                values = new List<object> { numPlayers };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.numPlayers.ToString(),
                    values = new List<object> { numPlayers }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithAvailableSlots(FilterOperator op, int availableSlots)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.availableSlots.ToString();
                values = new List<object> { availableSlots };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.availableSlots.ToString(),
                    values = new List<object> { availableSlots }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithSimulatorSlug(FilterOperator op, string simSlug)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.simSlug.ToString();
                values = new List<object> { simSlug };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.simSlug.ToString(),
                    values = new List<object> { simSlug }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithIsPrivateLobby(FilterOperator op, bool isPrivate)
        {
            if (op != FilterOperator.Equals && op != FilterOperator.NotEquals)
            {
                throw new ArgumentException(
                    $"Filter Operator {op} can only be Equals or NotEquals for boolean filters");
            }

            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = FilterKey.Private.ToString().ToLowerInvariant();
                values = new List<object> { isPrivate };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = FilterKey.Private.ToString().ToLowerInvariant(),
                    values = new List<object> { isPrivate }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithIntAttribute(FilterOperator op, IntAttributeIndex index, int value)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = index.ToString();
                values = new List<object> { value };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = index.ToString(),
                    values = new List<object> { value }
                };

                values.Add(newFilter);
            }

            return this;
        }

        public LobbyFilter WithStringAttribute(FilterOperator op, StringAttributeIndex index, string value)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't nest a filter inside another non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = filterToStringMapping[op];
                key = index.ToString();
                values = new List<object> { value };
            }
            else
            {
                var newFilter = new LobbyFilter()
                {
                    logicOperator = filterToStringMapping[op],
                    key = index.ToString(),
                    values = new List<object> { value }
                };

                values.Add(newFilter);
            }

            return this;
        }

        private LobbyFilter WithFilterGroup(FilterGroupOperator op)
        {
            if (IsNonFilterGroup())
            {
                throw new InvalidOperationException("Can't add a filter group to a non-filter group.");
            }

            if (IsEmptyFilter())
            {
                this.logicOperator = op.ToString().ToLowerInvariant();
                values = new List<object>();

                filterBuilderStack = new Stack<LobbyFilter>();
                filterBuilderStack.Push(this);

                return this;
            }

            var newFilter = new LobbyFilter()
            {
                logicOperator = op.ToString().ToLowerInvariant(), values = new List<object>()
            };

            values.Add(newFilter);
            newFilter.filterBuilderStack = filterBuilderStack;

            filterBuilderStack.Push(newFilter);

            return newFilter;
        }

        private bool IsEmptyFilter()
        {
            return logicOperator == null && key == null && values == null;
        }

        private bool IsNonFilterGroup()
        {
            return !string.IsNullOrEmpty(logicOperator) && key != null && values != null;
        }

        private bool IsFilterGroup()
        {
            return !string.IsNullOrEmpty(logicOperator) && key == null && values != null;
        }
    }

    public enum FilterGroupOperator
    {
        And,
        Or
    }

    public enum FilterOperator
    {
        Equals,
        NotEquals,
        LessThan,
        LessOrEqualThan,
        GreaterThan,
        GreaterOrEqualThan,
        Any,
        Between
    }

    public enum FilterKey
    {
        region,
        tag,
        maxPlayers,
        numPlayers,
        availableSlots,
        simSlug,
        Private
    }
}
