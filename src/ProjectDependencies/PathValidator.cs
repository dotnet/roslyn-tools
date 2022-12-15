using InputKit.Shared.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDependencies
{
    public class FilePathValidator : IValidation
    {
        public string Message { get; set; } = "Enter a valid path";

        public bool Validate(object value)
        {
            if (value is string text)
            {
                return Directory.Exists(text);
            }

            return false;
        }
    }
}
