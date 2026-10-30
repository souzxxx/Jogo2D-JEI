using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Abertura e objetivo (ESCOPO 5): o golpe que deu errado no Banco Central, a segunda chance e a revanche.
    /// Fica em Data/Narrative/Intro.asset. Textos do Nicolas; quem toca é o IntroDirector.
    /// Listas por tipo vão na ordem do ThiefType: Pé-na-Tábua, Gazua, Disquete, 171.
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Abertura", fileName = "Intro")]
    public class IntroDef : ScriptableObject
    {
        [Header("O golpe")]
        [Tooltip("Onde a firma antiga cai: o topo da escada.")]
        public HeistDef heist;
        [Tooltip("Nível da firma antiga (um de cada tipo, fora da sua crew).")]
        public int crewLevel = 8;
        [Tooltip("Primeiro nome de cada um da firma antiga, na ordem Pé-na-Tábua, Gazua, Disquete, 171.")]
        public string[] crewNames = { "Tião", "Bené", "Juninho", "Marcão" };

        [Header("Textos da abertura")]
        [TextArea(1, 3), Tooltip("Letreiro na tela preta, nos 3 primeiros segundos.")]
        public string caption = "Nova Guanabara, 1992. Avenida do Porto, 3 da manhã.";
        [Tooltip("Um bordão por tipo, quando a crew desce pro túnel. Mesma ordem dos nomes.")]
        public string[] barks =
        {
            "Motor ligado, chefia. E ninguém mexe no rádio.",
            "Cofre do Banco Central... hoje eu me aposento.",
            "Câmera tá em loop. Pode passar desfilando.",
            "O vigia é comigo. Já sou da família dele.",
        };
        [Tooltip("O grito da crew na porta, com a sirene tocando.")]
        public string oneMoreShout = "Só mais uma porta, chefia! Só mais uma!";
        public string headline = "BASTOS PEGA A FIRMA NO BANCO CENTRAL";
        [TextArea(1, 3)] public string subheadline = "Quadrilha insistiu depois da sirene; delegado diz que ninguém mais vai ouvir falar dela";
        [TextArea(2, 4), Tooltip("Bip da Dona Cida no fim da abertura. Substitui o bip de jogo novo.")]
        public string secondChanceBip = "Pegaram todo mundo, mas você escapou. A oficina é minha e os dois novatos também. Começa pela Lotérica do Seu Zé e, dessa vez, escuta a sirene.";

        [Header("Objetivo e revanche")]
        public string objective = "Objetivo: Banco Central";
        public string revengeHeadline = "A FIRMA VOLTOU: BANCO CENTRAL LIMPO; BASTOS PEDE FÉRIAS";
    }
}
