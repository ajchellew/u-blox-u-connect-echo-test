using System.Collections.Generic;

namespace UbloxTester
{
    // https://content.u-blox.com/sites/default/files/u-connectXpress-ATCommands-Manual_UBX-14044127.pdf
    // https://content.u-blox.com/sites/default/files/u-connectXpress_UserGuide_UBX-16024251.pdf

    internal class UConnectAtCommands
    {

        public const string ResponseOk = "OK";

        // start up mode - in case its been set to data. 0=AT
        public const string AtUmsm = "AT+UMSM=0";

        // device name
        public const string AtGmm = "AT+GMM";

        // manufacturer
        public const string AtGmi = "AT+GMI";

        // version
        public const string AtGmr = "AT+GMR";

        // local address 
        public const string AtUmla = "AT+UMLA=1";

        public static void CreateInitializationQueue(Queue<string> initializationCommandQueue)
        {
            // info about the NINA/ANNA
            initializationCommandQueue.Enqueue(AtUmsm);
            initializationCommandQueue.Enqueue(AtGmm);
            initializationCommandQueue.Enqueue(AtGmi);
            initializationCommandQueue.Enqueue(AtGmr);
            initializationCommandQueue.Enqueue(AtUmla);

            // Connected BT LE List
            initializationCommandQueue.Enqueue(AtUbtleList);
        }

        // list connected devices
        public const string AtUbtleList = "AT+UBTLELIST";
        // +UBTLELIST:0,5D45587725B2r /r OK

        // details of connected device
        public const string AtUbtleStat = "AT+UBTLESTAT=0";
            
        //+UBTLESTAT:1,32
        //+UBTLESTAT:2,0
        //+UBTLESTAT:3,2000  // timeout
        //+UBTLESTAT:4,23    // mtu
        //+UBTLESTAT:5,27    // pdu
        //+UBTLESTAT:6,0     // Data length extension
        //+UBTLESTAT:7,2
        //+UBTLESTAT:8,1
        //+UBTLESTAT:9,1
        //+UBTLESTAT:10,1
        //OK

        // switch to data mode
        public const string Ato1 = "ATO1";

        // switch to AT mode
        public const string Ato0 = "ATO0";
        public const string Escape = "+++";


        // Unsolicited
        public const string PeerConnected = "+UUDPC";
        public const string PeerDisconnected = "+UUDPD";

        public static string? ParseResponseForString(string[] responseParts)
        {
            if (responseParts.Length == 3 && responseParts[2] == UConnectAtCommands.ResponseOk)
            {
                return responseParts[1].Replace("\"", "");
            }
            return null;
        }

        public static string? ParseResponseForCommandPrefixedString(string[] responseParts)
        {
            if (responseParts.Length == 3 && responseParts[2] == UConnectAtCommands.ResponseOk)
            {
                return responseParts[1].Split(":")[1];
            }
            return null;
        }
    }
}
