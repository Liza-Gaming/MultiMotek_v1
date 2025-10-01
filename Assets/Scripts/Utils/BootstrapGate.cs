public static class BootstrapGate
{
    // מזוינים כשנכנסים מהתפריט; נצרכים פעם אחת בתחילת השלב
    private static bool _runStandaloneInitOnce;

    public static void ArmStandaloneInit() => _runStandaloneInitOnce = true;

    // מחזיר את המצב ומאפס אותו – חד-פעמי
    public static bool ConsumeStandaloneInitIfArmed()
    {
        bool armed = _runStandaloneInitOnce;
        _runStandaloneInitOnce = false;
        return armed;
    }
}