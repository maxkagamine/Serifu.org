using Python.Runtime;

namespace Serifu.ML;

internal static class PythonExtensions
{
    /// <summary>
    /// Creates a <see cref="PyList"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> from which to create a <see cref="PyList"/>.</param>
    /// <returns>A <see cref="PyList"/> that contains elements from the input sequence.</returns>
    public static PyList ToPyList<T>(this IEnumerable<T> source)
        => source.Select(x => x.ToPython()).ToPyList();

    /// <inheritdoc cref="ToPyList{T}(IEnumerable{T})"/>
    public static PyList ToPyList(this IEnumerable<PyObject> source)
    {
        PyList list = new();

        foreach (PyObject obj in source)
        {
            list.Append(obj);
        }

        return list;
    }
}
