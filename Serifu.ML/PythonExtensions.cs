// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

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
