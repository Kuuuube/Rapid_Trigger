using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Output;
using System;

namespace Rapid_Trigger
{
    [PluginName("Rapid Trigger")]
    public class Rapid_Trigger : IPositionedPipelineElement<IDeviceReport>
    {
        int delta_activate = 0;
        int delta_deactivate = 0;
        uint last_pressure = 0;
        PressedState pressed_state = PressedState.Reset;
        public IDeviceReport rapid_trigger(IDeviceReport input) {
            if (input is ITabletReport tabletReport) {
                return apply_rapid_trigger(tabletReport);
            }
            return input;
        }

        public ITabletReport apply_rapid_trigger(ITabletReport tabletReport) {
            delta_activate = Math.Clamp(delta_activate + ((int)tabletReport.Pressure - (int)last_pressure), 0, activation_sensitivity);
            delta_deactivate = Math.Clamp(delta_deactivate + -((int)tabletReport.Pressure - (int)last_pressure), 0, deactivation_sensitivity);

            last_pressure = tabletReport.Pressure;

            if (tabletReport.Pressure <= pressure_deactivation_threshold) {
                delta_activate = 0;
                delta_deactivate = 0;
                pressed_state = PressedState.Reset;
                return tabletReport;
            }

            if (tabletReport.Pressure >= pressure_activation_threshold && pressed_state == PressedState.Reset) {
                pressed_state = PressedState.Activated;
                return tabletReport;
            }

            if (delta_activate == activation_sensitivity) {
                pressed_state = PressedState.Activated;
                tabletReport.Pressure = tabletReport.Pressure + pressure_activation_threshold;
                return tabletReport;
            }

            if (delta_deactivate == deactivation_sensitivity) {
                pressed_state = PressedState.Deactivated;
                tabletReport.Pressure = 0;
                return tabletReport;
            }

            if (pressed_state == PressedState.Activated) {
                tabletReport.Pressure = tabletReport.Pressure + pressure_activation_threshold;
                return tabletReport;
            }

            if (pressed_state == PressedState.Deactivated) {
                tabletReport.Pressure = 0;
                return tabletReport;
            }

            return tabletReport;
        }

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value) {
            if (value is ITabletReport report) {
                report = (ITabletReport)Filter(report);
                value = report;
            }

            Emit?.Invoke(value);
        }

        public IDeviceReport Filter(IDeviceReport input) => rapid_trigger(input);

        public PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("Pressure Activation Threshold"), DefaultPropertyValue(1), ToolTip
            ("Rapid Trigger:\n\n" +
            "The raw pen pressure value where the pen tip binding is activated, regardless of the Activation Sensitivity or Deactivation Sensitivity.")]
        public uint pressure_activation_threshold { set; get; }

        [Property("Pressure Deactivation Threshold"), DefaultPropertyValue(0), ToolTip
            ("Rapid Trigger:\n\n" +
            "The raw pen pressure value where the pen tip binding is deactivated, regardless of the Activation Sensitivity or Deactivation Sensitivity.")]
        public uint pressure_deactivation_threshold { set; get; }

        [Property("Activation Sensitivity"), DefaultPropertyValue(250), ToolTip
            ("Rapid Trigger:\n\n" +
            "The raw pen pressure delta to activate the pen tip binding while the pen tip binding is deactivated.")]
        public int activation_sensitivity { set; get; }

        [Property("Deactivation Sensitivity"), DefaultPropertyValue(250), ToolTip
            ("Rapid Trigger:\n\n" +
            "The raw pen pressure delta to deactivate the pen tip binding while the pen tip binding is activated.")]
        public int deactivation_sensitivity { set; get; }
    }
    enum PressedState {
        Activated,
        Deactivated,
        Reset
    }
}