using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using RustyWires.Common;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    public static class BorrowTunnelViewModelHelpers
    {
        public static void CheckAllBorrowModesMatch<T>(this IEnumerable<IViewModel> selection, ICommandParameter parameter, BorrowMode match) where T : IBorrowTunnel
        {
            IEnumerable<T> borrowTunnels = selection.GetBorrowTunnels<T>();
            if (!borrowTunnels.Any())
            {
                return;
            }
            BorrowMode firstMode = borrowTunnels.First().BorrowMode;
            bool multipleModes = borrowTunnels.Any(bt => bt.BorrowMode != firstMode);
            ((ICheckableCommandParameter)parameter).IsChecked = firstMode == match && !multipleModes;
        }

        public static IEnumerable<T> GetBorrowTunnels<T>(this IEnumerable<IViewModel> selection) where T : IBorrowTunnel
        {
            return selection.Select(viewModel => viewModel.Model).OfType<T>();
        }

        public static void SetBorrowTunnelsMode<T>(this IEnumerable<T> borrowTunnels, BorrowMode borrowMode) where T : Element, IBorrowTunnel
        {
            if (borrowTunnels.Any())
            {
                using (IActiveTransaction transaction = (borrowTunnels.First()).TransactionManager.BeginTransaction("Set BorrowTunnel BorrowMode", TransactionPurpose.User))
                {
                    foreach (T borrowTunnel in borrowTunnels)
                    {
                        borrowTunnel.BorrowMode = borrowMode;
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
