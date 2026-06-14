using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Helpers;

public static class DocumentIdSearchHelper
{
    public static string ExtractDigits(string value) =>
        new string(value.Where(char.IsDigit).ToArray());

    public static IQueryable<User> WhereDocumentIdMatches(IQueryable<User> query, string? documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return query;

        var trimmed = documentId.Trim();
        var likePattern = "%" + trimmed + "%";
        var digitsOnly = ExtractDigits(trimmed);

        if (digitsOnly.Length == 0)
        {
            return query.Where(u =>
                u.DocumentId != null && EF.Functions.ILike(u.DocumentId, likePattern));
        }

        return query.Where(u =>
            u.DocumentId != null && (
                EF.Functions.ILike(u.DocumentId, likePattern)
                || u.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "").Contains(digitsOnly)));
    }

    public static IQueryable<StudentAssignment> WhereStudentDocumentIdMatches(
        IQueryable<StudentAssignment> query,
        string? documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return query;

        var trimmed = documentId.Trim();
        var likePattern = "%" + trimmed + "%";
        var digitsOnly = ExtractDigits(trimmed);

        if (digitsOnly.Length == 0)
        {
            return query.Where(sa =>
                sa.Student.DocumentId != null && EF.Functions.ILike(sa.Student.DocumentId, likePattern));
        }

        return query.Where(sa =>
            sa.Student.DocumentId != null && (
                EF.Functions.ILike(sa.Student.DocumentId, likePattern)
                || sa.Student.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "").Contains(digitsOnly)));
    }

    /// <summary>
    /// Búsqueda general (nombre, apellido, correo) con coincidencia flexible de cédula.
    /// </summary>
    public static IQueryable<User> WhereUserSearchMatches(IQueryable<User> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim();
        var likePattern = "%" + term + "%";
        var digitsOnly = ExtractDigits(term);

        if (digitsOnly.Length == 0)
        {
            return query.Where(u =>
                EF.Functions.ILike(u.Name, likePattern) ||
                EF.Functions.ILike(u.LastName, likePattern) ||
                (u.Email != null && EF.Functions.ILike(u.Email, likePattern)) ||
                (u.DocumentId != null && EF.Functions.ILike(u.DocumentId, likePattern)));
        }

        return query.Where(u =>
            EF.Functions.ILike(u.Name, likePattern) ||
            EF.Functions.ILike(u.LastName, likePattern) ||
            (u.Email != null && EF.Functions.ILike(u.Email, likePattern)) ||
            (u.DocumentId != null && (
                EF.Functions.ILike(u.DocumentId, likePattern)
                || u.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "").Contains(digitsOnly))));
    }

    public static IQueryable<StudentAssignment> WhereAssignmentStudentSearchMatches(
        IQueryable<StudentAssignment> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim();
        var likePattern = "%" + term + "%";
        var digitsOnly = ExtractDigits(term);

        if (digitsOnly.Length == 0)
        {
            return query.Where(sa =>
                EF.Functions.ILike(sa.Student.Name, likePattern) ||
                EF.Functions.ILike(sa.Student.LastName, likePattern) ||
                (sa.Student.Email != null && EF.Functions.ILike(sa.Student.Email, likePattern)) ||
                (sa.Student.DocumentId != null && EF.Functions.ILike(sa.Student.DocumentId, likePattern)));
        }

        return query.Where(sa =>
            EF.Functions.ILike(sa.Student.Name, likePattern) ||
            EF.Functions.ILike(sa.Student.LastName, likePattern) ||
            (sa.Student.Email != null && EF.Functions.ILike(sa.Student.Email, likePattern)) ||
            (sa.Student.DocumentId != null && (
                EF.Functions.ILike(sa.Student.DocumentId, likePattern)
                || sa.Student.DocumentId.Replace(".", "").Replace("-", "").Replace(" ", "").Contains(digitsOnly))));
    }
}
