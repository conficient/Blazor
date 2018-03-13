// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class BindTagHelperDescriptorProvider : ITagHelperDescriptorProvider
    {
        // Run after the component tag helper provider, because we need to see the results.
        public int Order { get; set; } = 1000;

        public RazorEngine Engine { get; set; }

        public void Execute(TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // This provider returns tag helper information for 'bind' which doesn't necessarily
            // map to any real component. Bind behaviors more like a macro, which can map a single LValue to
            // both a 'value' attribute and a 'value changed' attribute.
            //
            // User types: 
            //      <input type="text" bind="@FirstName"/>
            //
            // We generate:
            //      <input type="text" 
            //          value="@BindMethods.GetValue(FirstName)" 
            //          onchange="@BindMethods.SetValue(__value => FirstName = __value, FirstName)"/>
            //
            // This isn't very different from code the user could write themselves - thus the pronouncement
            // that bind is very much like a macro.
            //
            // A lot of the value that provide in this case is that the associations between the
            // elements, and the attributes aren't straightforward.
            //
            // For instance on <input type="text" /> we need to listen to 'value' and 'onchange',
            // but on <input type="checked" we need to listen to 'checked' and 'onchange'.
            //
            // We handle a few different cases here:
            //
            //  1.  When given an attribute like 'bind-value-changed="@FirstName"' we will generate the
            //      'value' attribute and 'changed' attribute. 
            //
            //      We don't do any transformation or inference for this case, because the developer has
            //      told us exactly what to do. This is the *full* form of bind, and should support any
            //      combination of elemement, component, and attributes.
            //
            //  2.  When given an attribute like 'bind-value="@FirstName"' we will generate the 'value'
            //      attribute and 'valuechanged' attribute - UNLESS we know better. For instance, with
            //      input tags we know that 'valuechanged' is likely not correct, the correct attribute
            //      is 'onchange
            //
            //      We will have to build up a list of mappings that describe the right thing to do for
            //      specific cases. Again, this is where we add substantial values to app developers. These
            //      kinds of things in the DOM aren't consistent, but presenting a uniform experience that
            //      generally does the right thing, we will make Blazor accessible to those without much
            //      experience doing DOM programming.
            //
            //  3.  When given an attribute like 'bind="@FirstName"' we will generate a value and change
            //      attribute solely based on the context. We need the context of an HTML tag to know
            //      what attributes to generate.
            //
            //      Similar to case #2, this should 'just work' from the users point of view. We expect
            //      using this syntax most frequently with input elements.
            //
            //  4.  For components, we have a bit of a special case. We can infer a syntax that matches
            //      case #2 based on property names. So if a component provides both 'Value' and 'ValueChanged'
            //      we will turn that into an instance of bind.
            //
            // So case #1 here is the most general case. Case #2 and #3 are data-driven based on element data
            // we have. Case #4 is data-driven based on component definitions.

            // Tag Helper defintion for case #1. This is the most general case. 
            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Bind.TagHelperKind, "Bind", BlazorApi.AssemblyName);
            builder.DisplayName = "Bind";
            builder.Documentation = "Bind-Documentation";

            builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.Bind.TagHelperKind);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Bind.RuntimeName;

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.Attribute(attribute =>
                {
                    attribute.Name = "bind-";
                    attribute.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
                });
            });

            builder.BindAttribute(attribute =>
            {
                attribute.AsDictionary("bind-", typeof(object).FullName);
                attribute.DisplayName = "Bind2";
                attribute.Documentation = "Bind-Documentation2";
                attribute.Name = "Bind";
                attribute.TypeName = typeof(IDictionary<string, object>).FullName;
            });

            context.Results.Add(builder.Build());
        }
    }
}
