import React from 'react';
import { VerificationResult } from '../../services/publicApi';

interface VerificationScoreCardProps {
  result: VerificationResult;
}

const gradeColors: Record<string, { bg: string; text: string; ring: string }> = {
  A: { bg: 'bg-green-100', text: 'text-green-700', ring: 'stroke-green-500' },
  B: { bg: 'bg-blue-100', text: 'text-blue-700', ring: 'stroke-blue-500' },
  C: { bg: 'bg-yellow-100', text: 'text-yellow-700', ring: 'stroke-yellow-500' },
  D: { bg: 'bg-orange-100', text: 'text-orange-700', ring: 'stroke-orange-500' },
  F: { bg: 'bg-red-100', text: 'text-red-700', ring: 'stroke-red-500' },
};

const VerificationScoreCard: React.FC<VerificationScoreCardProps> = ({ result }) => {
  const colors = gradeColors[result.grade] || gradeColors.F;
  const circumference = 2 * Math.PI * 54;
  const dashOffset = circumference - (result.overallScore / 100) * circumference;

  return (
    <div className="space-y-6">
      {/* Main Score */}
      <div className={`${colors.bg} rounded-2xl p-8 flex flex-col items-center`}>
        <div className="relative w-36 h-36">
          <svg className="w-36 h-36 transform -rotate-90" viewBox="0 0 120 120">
            <circle cx="60" cy="60" r="54" fill="none" stroke="#e5e7eb" strokeWidth="8" />
            <circle
              cx="60" cy="60" r="54" fill="none"
              className={colors.ring}
              strokeWidth="8"
              strokeLinecap="round"
              strokeDasharray={circumference}
              strokeDashoffset={dashOffset}
              style={{ transition: 'stroke-dashoffset 1s ease-in-out' }}
            />
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            <span className={`text-4xl font-bold ${colors.text}`}>{result.overallScore}</span>
            <span className="text-sm text-gray-500">/100</span>
          </div>
        </div>
        <div className={`mt-4 text-3xl font-bold ${colors.text}`}>Ocena: {result.grade}</div>
        <p className="mt-1 text-sm text-gray-600 text-center">{getGradeDescription(result.grade)}</p>
      </div>

      {/* Detail Sections */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Completeness */}
        {result.completeness && (
          <div className="bg-white border rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold text-gray-800">Kompletność</h3>
              <ScoreBadge score={result.completeness.score} />
            </div>
            <div className="space-y-2">
              {result.completeness.sections.map((section, i) => (
                <div key={i} className="flex items-center text-sm">
                  <span className={`mr-2 ${section.found ? 'text-green-500' : 'text-red-400'}`}>
                    {section.found ? '✓' : '✗'}
                  </span>
                  <span className={section.found ? 'text-gray-700' : 'text-gray-400'}>
                    {section.name}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Compliance */}
        {result.compliance && (
          <div className="bg-white border rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold text-gray-800">Zgodność z normami</h3>
              <ScoreBadge score={result.compliance.score} />
            </div>
            <div className="space-y-2">
              {result.compliance.norms.map((norm, i) => (
                <div key={i} className="flex items-center text-sm">
                  <span className={`mr-2 ${norm.referenced ? 'text-green-500' : 'text-red-400'}`}>
                    {norm.referenced ? '✓' : '✗'}
                  </span>
                  <span className={norm.referenced ? 'text-gray-700' : 'text-gray-400'}>
                    {norm.name}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Technical */}
        {result.technical && (
          <div className="bg-white border rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold text-gray-800">Specyfikacja techniczna</h3>
              <ScoreBadge score={result.technical.score} />
            </div>
            <div className="space-y-2 text-sm text-gray-700">
              <p>Mierzalne parametry: <strong>{result.technical.measurableParams}</strong></p>
              <p>Kwalifikatory: <strong>{result.technical.qualifiersUsed}</strong></p>
              {result.technical.issues.length > 0 && (
                <div className="mt-2">
                  <p className="font-medium text-orange-600">Uwagi:</p>
                  {result.technical.issues.map((issue, i) => (
                    <p key={i} className="text-orange-600 text-xs ml-2">- {issue}</p>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {/* Gap Analysis */}
        {result.gapAnalysis && (
          <div className="bg-white border rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold text-gray-800">Analiza braków</h3>
              <ScoreBadge score={result.gapAnalysis.score} />
            </div>
            <div className="space-y-2 text-sm">
              {result.gapAnalysis.missingSections.length > 0 && (
                <div>
                  <p className="font-medium text-red-600 mb-1">Brakujące sekcje:</p>
                  {result.gapAnalysis.missingSections.map((s, i) => (
                    <p key={i} className="text-red-500 text-xs ml-2">- {s}</p>
                  ))}
                </div>
              )}
              {result.gapAnalysis.recommendations.length > 0 && (
                <div>
                  <p className="font-medium text-blue-600 mb-1">Rekomendacje:</p>
                  {result.gapAnalysis.recommendations.map((r, i) => (
                    <p key={i} className="text-blue-600 text-xs ml-2">- {r}</p>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

const ScoreBadge: React.FC<{ score: number }> = ({ score }) => {
  const color = score >= 75 ? 'bg-green-100 text-green-700' :
    score >= 50 ? 'bg-yellow-100 text-yellow-700' :
    'bg-red-100 text-red-700';
  return <span className={`px-2 py-1 rounded-full text-xs font-bold ${color}`}>{score}%</span>;
};

function getGradeDescription(grade: string): string {
  switch (grade) {
    case 'A': return 'Doskonały dokument OPZ - spełnia wszystkie kluczowe wymagania';
    case 'B': return 'Dobry dokument OPZ - wymaga drobnych uzupełnień';
    case 'C': return 'Przeciętny dokument OPZ - wymaga istotnych poprawek';
    case 'D': return 'Słaby dokument OPZ - wymaga znacznych uzupełnień';
    case 'F': return 'Niewystarczający dokument OPZ - wymaga gruntownej przebudowy';
    default: return '';
  }
}

export default VerificationScoreCard;
