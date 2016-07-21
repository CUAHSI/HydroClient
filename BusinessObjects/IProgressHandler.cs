using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects.Models
{
    public interface IProgressHandler
    {
        /// <summary>
        /// Report progress
        /// </summary>
        /// <param name="percentage">Percentage of progress</param>
        /// <param name="state">State of progress</param>
        void ReportProgress(int percentage, object state);

        /// <summary>
        /// Check for cancel
        /// </summary>
        void CheckForCancel();

        /// <summary>
        /// Report any custom message
        /// </summary>
        /// <param name="message">Message to report</param>
        void ReportMessage(string message);

        /// <summary>
        /// CancellationToken
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}
