// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class ConditionalState
    {
        internal bool blocked;          // content not allowed through
        internal bool elsePermitted;    // condition not met so far

        internal ConditionalState(bool blocked, bool elsePermitted)
        {
            this.blocked = blocked;
            this.elsePermitted = elsePermitted;
        }
    }
}
