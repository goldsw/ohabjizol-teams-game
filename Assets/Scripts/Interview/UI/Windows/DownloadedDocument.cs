using Ohabjizol.UI;
namespace Ohabjizol.Interview
{
    public sealed class DownloadedDocument
    {
        public DocumentKey Key { get; }
        public ApplicantDocumentPacket PacketSnapshot { get; }
        public string DisplayName { get; }
        public int Order { get; }
        public WindowTaskbarDescriptor TaskbarInfo => new WindowTaskbarDescriptor(DisplayName, "PDF");
        internal DownloadedDocument(DocumentKey key, ApplicantDocumentPacket packet, int order)
        {
            Key = key;
            PacketSnapshot = packet;
            DisplayName = packet.DisplayName + " · " +
                (key.Kind == DocumentKind.Application ? "지원서" : "포트폴리오") + ".pdf";
            Order = order;
        }
    }
}
