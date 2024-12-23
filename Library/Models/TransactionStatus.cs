using System.ComponentModel;

namespace Library.Models
{
    public enum TransactionStatus
    {
        [Description("در انتظار نهایی سازی")]
        UnFinalized,

        [Description("در انتظار تایید")]
        PendingApproval,

        [Description("در انتظار تحویل")]
        PendingDelivery,

        [Description("رد شده")]
        Rejected,

        [Description("امانت گرفته شده")]
        Approved,

        [Description("بازگشت خورده")]
        Returned
    }
}
