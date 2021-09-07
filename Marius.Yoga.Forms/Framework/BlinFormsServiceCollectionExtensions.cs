﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlinForms.Framework
{
    public static class BlinFormsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a BlinForms component (typically from a .razor file) as the initial form to load when the
        /// application start.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRootFormContent<TComponent>(this IServiceCollection services)
            where TComponent : class, IComponent
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IBlinFormsRootFormContent, BlinFormsRootFormContent<TComponent>>();

            return services;
        }
    }
}
