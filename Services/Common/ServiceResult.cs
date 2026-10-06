// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng lớp trả kết quả Service hỗ trợ dẫn Controller.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

/// <summary>
/// Kết quả của một nghiệp vụ: thành công, hoặc danh sách lỗi theo tên thuộc tính
/// (khóa rỗng = lỗi chung). Controller đưa lỗi vào ModelState bằng AddTo().
/// </summary>
public class ServiceResult
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool Succeeded => _errors.Count == 0;
    public IReadOnlyDictionary<string, List<string>> Errors => _errors;
    public string FirstError => _errors.Values.SelectMany(m => m).FirstOrDefault() ?? string.Empty;

    public static ServiceResult Success() => new();
    public static ServiceResult Fail(string message) => Fail(string.Empty, message);
    public static ServiceResult Fail(string key, string message) => new ServiceResult().AddError(key, message);

    public ServiceResult AddError(string key, string message)
    {
        key ??= string.Empty;
        if (!_errors.TryGetValue(key, out var list))
            _errors[key] = list = new List<string>();
        list.Add(message);
        return this;
    }
}

/// <summary>Kết quả có kèm giá trị trả về khi thành công.</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private set; }

    public static ServiceResult<T> Ok(T value) => new() { Value = value };

    public static ServiceResult<T> Error(string key, string message)
    {
        var result = new ServiceResult<T>();
        result.AddError(key, message);
        return result;
    }
}

public static class ServiceResultExtensions
{
    /// <summary>Chép toàn bộ lỗi nghiệp vụ vào ModelState để View hiển thị.</summary>
    public static void AddTo(this ServiceResult result, ModelStateDictionary modelState)
    {
        foreach (var (key, messages) in result.Errors)
            foreach (var message in messages)
                modelState.AddModelError(key, message);
    }
}
