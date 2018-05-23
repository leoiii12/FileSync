using System;
using Autofac;
using Autofac.Core;

namespace FileSync
{
    public class LogRequestsModule : Module
    {
        public int depth;

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry,
            IComponentRegistration registration)
        {
            registration.Preparing += RegistrationOnPreparing;
            registration.Activating += RegistrationOnActivating;
            base.AttachToComponentRegistration(componentRegistry, registration);
        }

        private string GetPrefix()
        {
            if (depth == 0) return "";

            return new string('-', depth * 2) + " ";
        }

        private void RegistrationOnPreparing(object sender, PreparingEventArgs preparingEventArgs)
        {
            Console.WriteLine("{0}Resolving  {1}", GetPrefix(), preparingEventArgs.Component.Activator.LimitType);
            depth++;
        }

        private void RegistrationOnActivating(object sender, ActivatingEventArgs<object> activatingEventArgs)
        {
            depth--;
            Console.WriteLine("{0}Activating {1}", GetPrefix(), activatingEventArgs.Component.Activator.LimitType);
        }
    }
}