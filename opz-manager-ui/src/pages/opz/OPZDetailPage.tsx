import React, { useState, useMemo } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useOPZDocument } from '../../hooks/useOPZDocuments';
import { OPZRequirement, RequirementCompliance } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';

/** Remove [Device] prefix from requirement text for display */
function stripDevicePrefix(text: string): string {
  return text.replace(/^\[[^\]]+\]\s*/, '');
}

/** Status cell component with color and tooltip showing KB citations */
const ComplianceCell: React.FC<{ compliance: RequirementCompliance | undefined }> = ({ compliance }) => {
  const [showTooltip, setShowTooltip] = useState(false);

  if (!compliance || compliance.status === 'not_applicable') {
    return (
      <td className="px-3 py-2 text-center bg-gray-50 text-gray-400 text-xs border border-gray-200">
        —
      </td>
    );
  }

  const config = {
    met: { bg: 'bg-green-100', text: 'text-green-800', icon: '✓', label: 'Spełnia' },
    partial: { bg: 'bg-yellow-100', text: 'text-yellow-800', icon: '⚠', label: 'Wątpliwości' },
    not_met: { bg: 'bg-red-100', text: 'text-red-800', icon: '✗', label: 'Nie spełnia' },
  }[compliance.status] ?? { bg: 'bg-gray-50', text: 'text-gray-400', icon: '—', label: '' };

  const hasExplanation = !!compliance.explanation;

  return (
    <td
      className={`px-3 py-2 text-center ${config.bg} ${config.text} text-xs font-medium border border-gray-200 relative cursor-default`}
      onMouseEnter={() => hasExplanation && setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <span>{config.icon}</span>
      {showTooltip && hasExplanation && (
        <div className="absolute z-50 bottom-full left-1/2 -translate-x-1/2 mb-2 w-80 p-3 bg-gray-900 text-white text-xs rounded-lg shadow-xl pointer-events-none whitespace-pre-line leading-relaxed">
          <span className={`inline-block px-1.5 py-0.5 rounded text-[10px] font-bold mb-1.5 ${
            compliance.status === 'met' ? 'bg-green-600' :
            compliance.status === 'partial' ? 'bg-yellow-600' : 'bg-red-600'
          }`}>{config.label}</span>
          <br />
          {compliance.explanation}
          <div className="absolute top-full left-1/2 -translate-x-1/2 w-0 h-0 border-l-4 border-r-4 border-t-4 border-transparent border-t-gray-900" />
        </div>
      )}
    </td>
  );
};

const OPZDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { document, loading, analyze } = useOPZDocument(Number(id));

  // Group requirements by deviceCategory
  const groupedRequirements = useMemo(() => {
    if (!document) return [];
    const groups = new Map<string, OPZRequirement[]>();
    for (const req of document.requirements) {
      const cat = req.deviceCategory || 'Ogólne';
      if (!groups.has(cat)) groups.set(cat, []);
      groups.get(cat)!.push(req);
    }
    return Array.from(groups.entries());
  }, [document]);

  // Build compliance lookup: matchId -> requirementId -> compliance
  const complianceLookup = useMemo(() => {
    if (!document) return new Map<number, Map<number, RequirementCompliance>>();
    const lookup = new Map<number, Map<number, RequirementCompliance>>();
    for (const match of document.matches) {
      const reqMap = new Map<number, RequirementCompliance>();
      for (const rc of (match.requirementCompliances ?? [])) {
        reqMap.set(rc.requirementId, rc);
      }
      lookup.set(match.id, reqMap);
    }
    return lookup;
  }, [document]);

  const hasMatches = (document?.matches?.length ?? 0) > 0;

  if (loading) return <LoadingSpinner message="Ładowanie dokumentu..." />;
  if (!document) return <p className="text-gray-500">Dokument nie został znaleziony.</p>;

  const canAnalyze = document.analysisStatus !== 'Analizowanie';

  // Sort matches by score descending
  const sortedMatches = [...document.matches].sort((a, b) => b.matchScore - a.matchScore);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <Link to="/admin/opz" className="text-sm text-indigo-600 hover:text-indigo-800 mb-2 inline-block">
            ← Powrót do listy
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">{document.filename}</h1>
          <p className="text-sm text-gray-500 mt-1">
            Wgrano: {new Date(document.uploadDate).toLocaleString('pl-PL')} | Status: {document.analysisStatus}
          </p>
        </div>
        <button
          onClick={analyze}
          disabled={!canAnalyze}
          className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
        >
          {document.analysisStatus === 'Analizowanie' ? 'Analizowanie...' : 'Analizuj dokument'}
        </button>
      </div>

      {/* Compliance Matrix Table */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">
          Macierz zgodności wymagań ({document.requirements.length} wymagań
          {hasMatches ? `, ${sortedMatches.length} urządzeń` : ''})
        </h2>

        {document.requirements.length === 0 ? (
          <p className="text-gray-500 bg-white rounded-lg border p-4">Brak wymagań</p>
        ) : (
          <div className="overflow-x-auto border border-gray-200 rounded-lg shadow-sm">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="sticky left-0 z-20 bg-gray-50 px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border border-gray-200 w-12">
                    Lp.
                  </th>
                  <th className="sticky left-12 z-20 bg-gray-50 px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border border-gray-200 w-24">
                    Kategoria
                  </th>
                  <th className="sticky left-36 z-20 bg-gray-50 px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border border-gray-200 min-w-[300px]">
                    Wymaganie
                  </th>
                  {hasMatches && sortedMatches.map((match) => (
                    <th
                      key={match.id}
                      className="px-3 py-3 text-center text-xs font-medium text-gray-700 uppercase tracking-wider border border-gray-200 min-w-[130px] bg-gray-50"
                    >
                      <Link
                        to={`/admin/equipment/${match.modelId}`}
                        className="text-indigo-600 hover:text-indigo-800 font-semibold normal-case"
                      >
                        {match.manufacturerName} {match.modelName}
                      </Link>
                      <div className="mt-1 flex items-center justify-center gap-1">
                        <div className="w-16 h-1.5 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className="h-full bg-indigo-600 rounded-full"
                            style={{ width: `${match.matchScore * 100}%` }}
                          />
                        </div>
                        <span className="text-xs font-bold text-gray-600">
                          {(match.matchScore * 100).toFixed(0)}%
                        </span>
                      </div>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="bg-white">
                {(() => {
                  let rowNumber = 0;
                  return groupedRequirements.map(([deviceCategory, requirements]) => (
                    <React.Fragment key={deviceCategory}>
                      {/* Device group header row */}
                      <tr className="bg-indigo-50">
                        <td
                          colSpan={3 + (hasMatches ? sortedMatches.length : 0)}
                          className="px-3 py-2 text-sm font-bold text-indigo-900 border border-gray-200"
                        >
                          {deviceCategory}
                        </td>
                      </tr>
                      {/* Requirement rows */}
                      {requirements.map((req) => {
                        rowNumber++;
                        return (
                          <tr key={req.id} className="hover:bg-gray-50">
                            <td className="sticky left-0 z-10 bg-white px-3 py-2 text-sm text-gray-500 border border-gray-200 text-center">
                              {rowNumber}
                            </td>
                            <td className="sticky left-12 z-10 bg-white px-3 py-2 border border-gray-200">
                              <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${
                                req.requirementType === 'Technical' ? 'bg-blue-100 text-blue-700' :
                                req.requirementType === 'Performance' ? 'bg-purple-100 text-purple-700' :
                                req.requirementType === 'Compliance' ? 'bg-orange-100 text-orange-700' :
                                'bg-gray-100 text-gray-600'
                              }`}>
                                {req.requirementType}
                              </span>
                            </td>
                            <td className="sticky left-36 z-10 bg-white px-3 py-2 text-sm text-gray-800 border border-gray-200">
                              {stripDevicePrefix(req.requirementText)}
                            </td>
                            {hasMatches && sortedMatches.map((match) => (
                              <ComplianceCell
                                key={match.id}
                                compliance={complianceLookup.get(match.id)?.get(req.id)}
                              />
                            ))}
                          </tr>
                        );
                      })}
                    </React.Fragment>
                  ));
                })()}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Equipment summary cards (below table, for compliance descriptions) */}
      {hasMatches && (
        <div className="mb-8">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">
            Podsumowanie dopasowań
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {sortedMatches.map((match) => (
              <div key={match.id} className="bg-white rounded-lg border border-gray-200 p-4">
                <div className="flex items-center justify-between mb-2">
                  <Link
                    to={`/admin/equipment/${match.modelId}`}
                    className="font-medium text-indigo-600 hover:text-indigo-800"
                  >
                    {match.manufacturerName} {match.modelName}
                  </Link>
                  <span className="text-sm font-bold text-gray-700">
                    {(match.matchScore * 100).toFixed(0)}%
                  </span>
                </div>
                <p className="text-xs text-gray-500 mb-2">{match.typeName}</p>
                <p className="text-sm text-gray-600">{match.complianceDescription}</p>
                {/* Quick stats */}
                {match.requirementCompliances && match.requirementCompliances.length > 0 && (
                  <div className="mt-3 flex gap-3 text-xs">
                    <span className="text-green-700">
                      ✓ {match.requirementCompliances.filter(c => c.status === 'met').length}
                    </span>
                    <span className="text-yellow-700">
                      ⚠ {match.requirementCompliances.filter(c => c.status === 'partial').length}
                    </span>
                    <span className="text-red-700">
                      ✗ {match.requirementCompliances.filter(c => c.status === 'not_met').length}
                    </span>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default OPZDetailPage;
