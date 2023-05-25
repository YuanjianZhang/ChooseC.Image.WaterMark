﻿using Microsoft.Extensions.Configuration;
using System.CommandLine.Binding;

namespace ChooseC.Image.WaterMark.ConsoleApp.CustomBinder
{
    public class ConfigCustomBinder : BinderBase<IConfigurationRoot>
    {
        protected override IConfigurationRoot GetBoundValue(
         BindingContext bindingContext) => GetRoot(bindingContext);

        IConfigurationRoot GetRoot(BindingContext bindingContext)
        {
            var root = new ConfigurationBuilder()
                .AddJsonFile("appsettings.develop.json", true, true)
                .AddJsonFile("appsettings.json", false, true).Build();
            return root;
        }
    }
}
