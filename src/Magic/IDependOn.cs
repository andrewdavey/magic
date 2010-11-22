namespace Magic
{
    // Magic marker interfaces contain no members.
    // They are just to signal when to apply code generation.
    // (Generic interfaces provide a nicer syntax than attributes in this situation.)

    /// <summary>
    /// Signals Magic to generate a public constructor for the class 
    /// taking a dependency <typeparamref name="T"/>.
    /// </summary>
    public interface IDependOn<T> { }

    /// <summary>
    /// Signals Magic to generate a public constructor for the class 
    /// taking a dependency <typeparamref name="T1"/> and <typeparam name="T2"/>.
    /// </summary>
    public interface IDependOn<T1, T2> { }

    /// <summary>
    /// Signals Magic to generate a public constructor for the class 
    /// taking a dependency <typeparamref name="T1"/>, <typeparam name="T2"/> and <typeparam name="T3"/>.
    /// </summary>
    public interface IDependOn<T1, T2, T3> { }

    /// <summary>
    /// Signals Magic to generate a public constructor for the class 
    /// taking a dependency <typeparamref name="T1"/>, <typeparam name="T2"/>, 
    /// <typeparam name="T3"/> and <typeparam name="T4"/>.
    /// </summary>
    public interface IDependOn<T1, T2, T3, T4> { }

    /// <summary>
    /// Signals Magic to generate a public constructor for the class 
    /// taking a dependency <typeparamref name="T1"/>, <typeparam name="T2"/>, 
    /// <typeparam name="T3"/>, <typeparam name="T4"/> and <typeparam name="T5"/>.
    /// </summary>
    public interface IDependOn<T1, T2, T3, T4, T5> { }
}
