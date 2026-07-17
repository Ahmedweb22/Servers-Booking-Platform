using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace Shtbly.Utilities
{
    public class HashidOutboundParameterTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value)
        {
            if (value == null) return null;

            if (value is int intValue)
            {
                return UrlObfuscator.Encrypt(intValue);
            }

            if (value is string strValue && int.TryParse(strValue, out var parsedInt))
            {
                return UrlObfuscator.Encrypt(parsedInt);
            }

            return value.ToString();
        }
    }

    public class EncryptedIdModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Check if it's already an int (some links might be hardcoded to ints)
            if (int.TryParse(value, out var intValue))
            {
                bindingContext.Result = ModelBindingResult.Success(intValue);
                return Task.CompletedTask;
            }

            // Try decrypting
            var decrypted = UrlObfuscator.Decrypt(value);
            if (decrypted > 0)
            {
                bindingContext.Result = ModelBindingResult.Success(decrypted);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid ID format.");
            return Task.CompletedTask;
        }
    }

    public class EncryptedIdModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Metadata.ModelType == typeof(int) || context.Metadata.ModelType == typeof(int?))
            {
                // Apply only to parameters named 'id' or ending with 'Id' (like 'bookingId')
                if (context.Metadata.Name != null && 
                    (context.Metadata.Name.Equals("id", StringComparison.OrdinalIgnoreCase) || 
                     context.Metadata.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    return new BinderTypeModelBinder(typeof(EncryptedIdModelBinder));
                }
            }

            return null;
        }
    }
}
