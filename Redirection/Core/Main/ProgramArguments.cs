using Dan200.Core.Assets;
using System.Text;

namespace Dan200.Core.Main
{
    public class ProgramArguments : KeyValuePairs
    {
        private string m_representation;

        public ProgramArguments(string[] args)
        {
            var representation = new StringBuilder();
            string lastOption = null;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (lastOption != null)
                    {
                        Set(lastOption, true);
                        representation.Append("-" + lastOption + " ");
                    }
                    lastOption = arg.Substring(1);
                }
                else if (lastOption != null)
                {
                    Set(lastOption, arg);
                    representation.Append("-" + lastOption + " " + arg + " ");
                    lastOption = null;
                }
            }
            if (lastOption != null)
            {
                Set(lastOption, true);
                representation.Append("-" + lastOption + " ");
            }
            Modified = false;
            m_representation = representation.ToString().TrimEnd();
        }

        public override string ToString()
        {
            return m_representation;
        }
    }
}
