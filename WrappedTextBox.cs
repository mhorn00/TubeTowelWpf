using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace TubeTowelAppWpf {
    internal class WrappedTextBox : TextBox {
        //This is to prevent the on screen keyboard from opening with touch input....
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new FrameworkElementAutomationPeer(this);
        }
    }
}
