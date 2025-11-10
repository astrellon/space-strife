using System.Text;

#nullable enable

namespace Orbits
{
    public class StringAppender
    {
        #region Fields
        public readonly StringBuilder Builder = new ();
        private readonly string separator;
        private bool first = true;
        #endregion

        #region Constructor
        public StringAppender(string separator)
        {
            this.separator = separator;
        }
        #endregion

        #region Methods
        public void Append(string entry)
        {
            if (!first)
            {
                this.Builder.Append(this.separator);
            }
            this.Builder.Append(entry);
            first = false;
        }

        public string GetString()
        {
            return this.Builder.ToString();
        }

        public void Clear()
        {
            this.Builder.Clear();
            this.first = true;
        }
        #endregion
    }
}