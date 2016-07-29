using RazorEngine.Templating;

namespace PipelineAuditToolkit.Resources
{
    public class CustomTemplateBase<T> : TemplateBase<T>
    {
        public new T Model
        {
            get { return base.Model; }
            set { base.Model = value; }
        }

        public CustomTemplateBase()
        {
        }
    }
}
