namespace TransactionFraudWorker.Options;

public sealed class KafkaDlqSettings
{
    public string? BootstrapServers { get; set; }

    public string Topic { get; set; } = string.Empty;

    public DlqAcks Acks { get; set; } = DlqAcks.Leader;
}

public enum DlqAcks
{
    Leader,
    All,
}
