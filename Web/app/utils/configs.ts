export interface ClassGradeRuleExampleNote {
  label: string
  value: string
  used: boolean
}

export type ClassGradeRuleTerm =
  | { type: 'fraction', numerator: string, denominator: string }
  | { type: 'text', value: string }

export interface ClassGradeRuleExample {
  notes: ClassGradeRuleExampleNote[]
  hint: string
  calculation: ClassGradeRuleTerm[]
  result: string
}

export interface ClassGradeRuleInfo {
  label: string
  description: string
  example: ClassGradeRuleExample
}

export const classGradeRules: Record<string, ClassGradeRuleInfo> = {
  BestTwoOfThree: {
    label: 'Média das duas maiores entre N1, N2 e N3',
    description: 'O aluno recebe três notas e só as duas maiores entram na média final — a menor é descartada.',
    example: {
      notes: [
        { label: 'N1', value: '8,0', used: true },
        { label: 'N2', value: '5,0', used: false },
        { label: 'N3', value: '7,0', used: true },
      ],
      hint: 'A N2 é a menor das três, então é descartada.',
      calculation: [
        { type: 'fraction', numerator: '8,0 + 7,0', denominator: '2' },
      ],
      result: '7,50',
    },
  },
  AverageOfTwo: {
    label: 'Média de N1 e N2',
    description: 'A instituição usa só duas notas, e a média final é a média simples entre elas.',
    example: {
      notes: [
        { label: 'N1', value: '8,0', used: true },
        { label: 'N2', value: '5,0', used: true },
      ],
      hint: 'As duas notas entram na média, com o mesmo peso.',
      calculation: [
        { type: 'fraction', numerator: '8,0 + 5,0', denominator: '2' },
      ],
      result: '6,50',
    },
  },
  AverageOfThree: {
    label: 'Média de N1, N2 e N3',
    description: 'As três notas entram na média final, todas com o mesmo peso.',
    example: {
      notes: [
        { label: 'N1', value: '8,0', used: true },
        { label: 'N2', value: '5,0', used: true },
        { label: 'N3', value: '7,0', used: true },
      ],
      hint: 'Nenhuma nota é descartada.',
      calculation: [
        { type: 'fraction', numerator: '8,0 + 5,0 + 7,0', denominator: '3' },
      ],
      result: '6,67',
    },
  },
  AverageOrThird: {
    label: 'Média de N1 e N2, ou N3',
    description: 'Vale a média de N1 e N2, a não ser que a nota da N3 seja maior.',
    example: {
      notes: [
        { label: 'N1', value: '8,0', used: false },
        { label: 'N2', value: '5,0', used: false },
        { label: 'N3', value: '7,0', used: true },
      ],
      hint: 'A média de N1 e N2 seria 6,50, mas a N3 é maior.',
      calculation: [
        { type: 'fraction', numerator: '8,0 + 5,0', denominator: '2' },
        { type: 'text', value: '=' },
        { type: 'text', value: '6,50' },
        { type: 'text', value: '<' },
        { type: 'text', value: 'N3 7,0' },
      ],
      result: '7,00',
    },
  },
}

export const classGradeRuleOptions = Object.entries(classGradeRules)
  .map(([value, rule]) => ({ label: rule.label, value }))
