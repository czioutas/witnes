import React from 'react';

const ComingSoon: React.FC = () => {
  return (
    <div className="flex flex-col items-center justify-center h-full">
      <h1 className="text-4xl font-bold text-foreground mb-4">Coming Soon</h1>
      <p className="text-lg text-muted-foreground">
        We are working hard to bring you this feature. Stay tuned!
      </p>
    </div>
  );
};

export default ComingSoon;
