using System;
using System.Collections.Generic;

namespace OpenAutomate.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Validation errors by field
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; }

        public ValidationException() : base("One or more validation errors occurred")
        {
            Errors = new Dictionary<string, List<string>>();
        }

        public ValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, List<string>>();
        }

        public ValidationException(Dictionary<string, List<string>> errors) 
            : base("One or more validation errors occurred")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error) : base(error)
        {
            Errors = new Dictionary<string, List<string>>
            {
                { field, new List<string> { error } }
            };
        }
    }
} 