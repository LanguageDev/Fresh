// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fresh.Common;

/// <summary>
/// A result indicating either a success or error.
/// </summary>
/// <typeparam name="TOk">The ok (success) type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public readonly struct Result<TOk, TError> : IEquatable<Result<TOk, TError>>
{
    /// <summary>
    /// True, if this result is a successful alternative.
    /// </summary>
    public readonly bool IsOk;

    /// <summary>
    /// True, if this result is an error alternative.
    /// </summary>
    public bool IsError => !this.IsOk;

    /// <summary>
    /// Unwraps the success value of the result.
    /// </summary>
    public TOk UnwrapOk => this.IsOk
                         ? this.okValue!
                         : throw new InvalidOperationException("Tried to unwrap Result with an error value!");

    /// <summary>
    /// Unwraps the error value of the result.
    /// </summary>
    public TError UnwrapError => this.IsError
                               ? this.errorValue!
                               : throw new InvalidOperationException("Tried to unwrap the error of a Result with an ok value!");

    private readonly TOk? okValue;
    private readonly TError? errorValue;

    public Result(TOk ok)
    {
        this.IsOk = true;
        this.okValue = ok;
        this.errorValue = default;
    }

    public Result(TError error)
    {
        this.IsOk = false;
        this.okValue = default;
        this.errorValue = error;
    }

    public static implicit operator Result<TOk, TError>(TOk ok) => new(ok);

    public static implicit operator Result<TOk, TError>(TError error) => new(error);

    public override bool Equals(object? obj) =>
        obj is Result<TOk, TError> other && this.Equals(other);

    public bool Equals(Result<TOk, TError> other) => this.IsOk
        ? (other.IsOk && this.okValue!.Equals(other.okValue))
        : (other.IsError && this.errorValue!.Equals(other.errorValue));

    public static bool operator ==(Result<TOk, TError> left, Result<TOk, TError> right) => left.Equals(right);

    public static bool operator !=(Result<TOk, TError> left, Result<TOk, TError> right) => !(left == right);

    public override int GetHashCode() => this.IsOk
        ? this.okValue!.GetHashCode()
        : this.errorValue!.GetHashCode();

    /// <summary>
    /// Unwraps the success value of this result or returns a default, if it's an error result.
    /// </summary>
    /// <param name="default">The default value to return.</param>
    /// <returns>The ok variant, if <see cref="IsOk"/> is true, <paramref name="default"/> otherwise.</returns>
    public TOk UnwrapOkOr(TOk @default) => this.IsOk ? this.okValue! : @default;

    /// <summary>
    /// Unwraps the error value of this result or returns a default, if it's an ok result.
    /// </summary>
    /// <param name="default">The default value to return.</param>
    /// <returns>The error variant, if <see cref="IsError"/> is true, <paramref name="default"/> otherwise.</returns>
    public TError UnwrapErrorOr(TError @default) => this.IsError ? this.errorValue! : @default;

    /// <summary>
    /// Maps the ok value of this result using a function.
    /// </summary>
    /// <typeparam name="TNewOk">The new ok value type.</typeparam>
    /// <param name="func">The function to convert the ok value.</param>
    /// <returns>A new result that has its ok value converted using <paramref name="func"/>, if this was an ok result.</returns>
    public Result<TNewOk, TError> MapOk<TNewOk>(Func<TOk, TNewOk> func) => this.IsOk
        ? new(func(this.okValue!))
        : new(this.errorValue!);

    /// <summary>
    /// Maps the error value of this result using a function.
    /// </summary>
    /// <typeparam name="TNewError">The new error value type.</typeparam>
    /// <param name="func">The function to convert the error value.</param>
    /// <returns>A new result that has its error value converted using <paramref name="func"/>, if this was an error result.</returns>
    public Result<TOk, TNewError> MapError<TNewError>(Func<TError, TNewError> func) => this.IsError
        ? new(func(this.errorValue!))
        : new(this.okValue!);

    /// <summary>
    /// Chains another operation that returs a result, if this is an ok variant. 
    /// </summary>
    /// <typeparam name="TNewOk">The new success type in case the operation succeeds.</typeparam>
    /// <param name="func">The executed operation.</param>
    /// <returns>The result of <paramref name="func"/>, if this is an ok result, unchanged otherwise.</returns>
    public Result<TNewOk, TError> AndThen<TNewOk>(Func<TOk, Result<TNewOk, TError>> func) => this.IsOk
        ? func(this.okValue!)
        : new(this.errorValue!);

    /// <summary>
    /// Chains another operation that returs a result, if this is an error variant. 
    /// </summary>
    /// <typeparam name="TNewError">The new error type in case the fails succeeds.</typeparam>
    /// <param name="func">The executed operation.</param>
    /// <returns>The result of <paramref name="func"/>, if this is an error result, unchanged otherwise.</returns>
    public Result<TOk, TNewError> OrElse<TNewError>(Func<TError, Result<TOk, TNewError>> func) => this.IsError
        ? func(this.errorValue!)
        : new(this.okValue!);
}
