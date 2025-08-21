using System;

namespace BehaviourTreeLogic.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class BTHelpAttribute : Attribute
    {
        public readonly string HelpText;

        public BTHelpAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }
}