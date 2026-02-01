namespace Prakrishta.Infrastructure.Helper
{
    using System;
    using System.Collections.Generic;

    public sealed class DecoratorPipelineBuilder
    {
        private readonly List<Type> _decorators = new();

        public DecoratorPipelineBuilder Use(Type decoratorType)
        {
            _decorators.Add(decoratorType);
            return this;
        }

        internal IReadOnlyList<Type> Build() => _decorators;
    }
}
