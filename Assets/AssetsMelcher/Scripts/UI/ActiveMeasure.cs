namespace KitchenDesign
{
    using KitchenDesign.Measures;
    using UnityEngine;

    public struct ActiveMeasure
    {
        public ActiveMeasure(Measure measure, Vector2 canvasLabelPivot)
        {
            this.measure = measure;
            this.canvasLabelPivot = canvasLabelPivot;
        }

        public Measure measure;
        public Vector2 canvasLabelPivot;
    }
}
