using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace BlastSearch
{

    public class InputSequenceNew
    {
        private string ID = string.Empty;
        private string Sequence = string.Empty;
        private string Quality = string.Empty;
        private string Target = string.Empty;
        private string Status = string.Empty;


        public string id
        {
            get { return ID; }
            set { ID = value; }
        }

        public string sequence
        {
            get { return Sequence; }
            set { Sequence = value; }
        }


        public string quality
        {
            get { return Quality; }
            set { Quality = value; }
        }

        public string target
        {
            get { return Target; }
            set { Target = value; }
        }

        public string status
        {
            get { return Status; }
            set { Status = value; }
        }

        public int GetLength()
        {
            return id.ToString().Length + sequence.ToString().Length + quality.ToString().Length + target.ToString().Length + status.ToString().Length;
        }           
      
    }
}
