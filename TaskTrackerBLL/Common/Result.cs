using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.Common
{
    public class Result
    {
        public bool Succeeded { get; }

        public string? Error { get; }

        public IReadOnlyList<string> Errors { get; }

        protected Result(bool succeeded, IReadOnlyList<string> errors)
        {
            Succeeded = succeeded;
            Errors = errors;
            Error = errors.Count > 0 ? errors[0] : null;
        }

        public static Result Success() => new(true, Array.Empty<string>());

        public static Result Failure(string error) => new(false, new List<string> { error });

        public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToList());
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        protected Result(bool succeeded, T? value, IReadOnlyList<string> errors)
            : base(succeeded, errors)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());

        public static new Result<T> Failure(string error) => new(false, default, new List<string> { error });

        public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToList());
    }
}
